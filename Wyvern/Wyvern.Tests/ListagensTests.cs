using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

/// <summary>
/// Listas paginadas (campanhas, personagens, sessões) e o painel: a página certa, os totais certos e nada além do
/// que o usuário já via nas listas completas.
/// </summary>
public class ListagensTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public ListagensTests(WyvernApiFactory factory) => _factory = factory;

    // ---------- apoio ----------

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre, string nome, string sistema = "D&D 5e")
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome, sistema });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static async Task<int> CriarSessaoAsync(HttpClient mestre, int campanhaId, int numero, DateTime? agendada = null)
    {
        var resposta = await mestre.PostAsJsonAsync("/Sessao", new
        {
            numeroSessao = numero, nome = $"Sessão {numero}", dataSessao = DateTime.Today.AddDays(-numero), dataAgendada = agendada, obs = "", campanhaId,
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
    }

    private static async Task<int> CriarPersonagemAsync(HttpClient cliente, int campanhaId, string nome, int tipoId = 1, string descricao = "")
    {
        var resposta = await cliente.PostAsJsonAsync("/Personagem", new
        {
            nome, campanhaId, tipoId, descricao,
            personagemNpc = Fichas.BlocoSe(tipoId),
            personagemPlayer = new { classe = "Guerreiro", nivel = 1, raca = "Humano", antecedente = "Soldado", subclasse = "Campeão" },
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 10, vidaAtual = 10, dadoVidaMaximo = "1d10", deslocamento = "9m" },
        });
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    private static Task<JsonElement> GetAsync(HttpClient cliente, string url) => cliente.GetFromJsonAsync<JsonElement>(url);

    private static List<string> Nomes(JsonElement pagina) =>
        pagina.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("nome").GetString()!).ToList();

    // ---------- campanhas ----------

    [Fact]
    public async Task Campanhas_vem_em_paginas_da_mais_nova_para_a_mais_antiga()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        foreach (var nome in new[] { "C1", "C2", "C3", "C4", "C5" }) await CriarCampanhaAsync(mestre, nome);

        var primeira = await GetAsync(mestre, "/Campanha/pagina?pagina=1&tamanho=2");
        var terceira = await GetAsync(mestre, "/Campanha/pagina?pagina=3&tamanho=2");

        Assert.Equal(5, primeira.GetProperty("total").GetInt32());
        Assert.Equal(3, primeira.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(new[] { "C5", "C4" }, Nomes(primeira));
        Assert.Equal(new[] { "C1" }, Nomes(terceira));
        Assert.Equal("Mestre", primeira.GetProperty("itens")[0].GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Campanha_na_lista_traz_os_numeros_da_capa_e_a_busca_filtra_no_servidor()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var vael = await CriarCampanhaAsync(mestre, "A Queda de Vael");
        await CriarCampanhaAsync(mestre, "Brumavale", sistema: "Tormenta20");
        await CriarSessaoAsync(mestre, vael, 1);
        await CriarSessaoAsync(mestre, vael, 2);
        await CriarPersonagemAsync(mestre, vael, "Thorin");
        await CriarPersonagemAsync(mestre, vael, "Goblin", tipoId: 3);

        var porNome = await GetAsync(mestre, "/Campanha/pagina?busca=vael");
        var porSistema = await GetAsync(mestre, "/Campanha/pagina?busca=TORMENTA");

        var item = Assert.Single(porNome.GetProperty("itens").EnumerateArray());
        Assert.Equal(2, item.GetProperty("totalSessoes").GetInt32());
        Assert.Equal(1, item.GetProperty("totalHerois").GetInt32()); // o monstro não conta como herói
        Assert.Equal(new[] { "Brumavale" }, Nomes(porSistema));
    }

    [Fact]
    public async Task Pagina_e_tamanho_fora_dos_limites_sao_ajustados()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        await CriarCampanhaAsync(mestre, "Única");

        var pedido = await GetAsync(mestre, "/Campanha/pagina?pagina=0&tamanho=1000");

        Assert.Equal(1, pedido.GetProperty("pagina").GetInt32());
        Assert.Equal(48, pedido.GetProperty("tamanhoPagina").GetInt32());
        var alem = await GetAsync(mestre, "/Campanha/pagina?pagina=9");
        Assert.Empty(alem.GetProperty("itens").EnumerateArray());
        Assert.Equal(1, alem.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Estranho_nao_ve_campanhas_sessoes_nem_personagens_de_outra_mesa()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre, "Segredo");
        await CriarSessaoAsync(mestre, campanhaId, 1);
        await CriarPersonagemAsync(mestre, campanhaId, "Escondido");

        Assert.Equal(0, (await GetAsync(estranho, "/Campanha/pagina")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await GetAsync(estranho, "/Sessao/pagina")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await GetAsync(estranho, $"/Sessao/pagina?campanhaId={campanhaId}")).GetProperty("total").GetInt32());
        var personagens = await GetAsync(estranho, $"/Personagem/pagina?campanhaId={campanhaId}");
        Assert.Equal(0, personagens.GetProperty("total").GetInt32());
        Assert.Equal(0, personagens.GetProperty("totalHerois").GetInt32());
        var painel = await GetAsync(estranho, "/Dashboard/resumo");
        Assert.Equal(0, painel.GetProperty("totalCampanhas").GetInt32());
        Assert.Equal(JsonValueKind.Null, painel.GetProperty("destaque").ValueKind);
    }

    [Fact]
    public async Task Listas_paginadas_exigem_login()
    {
        var anonimo = _factory.CreateClient();
        foreach (var url in new[] { "/Campanha/pagina", "/Personagem/pagina", "/Sessao/pagina", "/Dashboard/resumo" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(url)).StatusCode);
    }

    // ---------- personagens ----------

    [Fact]
    public async Task Personagens_separados_por_aba_com_contagens_para_as_abas_e_os_filtros()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var vael = await CriarCampanhaAsync(mestre, "Vael");
        var bruma = await CriarCampanhaAsync(mestre, "Bruma");
        await EntrarAsync(mestre, jogador, vael);
        await CriarPersonagemAsync(jogador, vael, "Lyra");
        await CriarPersonagemAsync(mestre, vael, "Thorin");
        await CriarPersonagemAsync(mestre, bruma, "Aldric");
        await CriarPersonagemAsync(mestre, vael, "Goblin", tipoId: 3);
        await CriarPersonagemAsync(mestre, vael, "Taverneiro", tipoId: 2);

        var herois = await GetAsync(mestre, "/Personagem/pagina?tipo=herois");
        var monstros = await GetAsync(mestre, "/Personagem/pagina?tipo=monstros");

        Assert.Equal(3, herois.GetProperty("total").GetInt32());
        Assert.Equal(3, herois.GetProperty("totalHerois").GetInt32());
        Assert.Equal(2, herois.GetProperty("totalMonstros").GetInt32());
        // por campanha e, nela, por nome
        Assert.Equal(new[] { "Aldric", "Lyra", "Thorin" }, Nomes(herois));
        var filtros = herois.GetProperty("porCampanha").EnumerateArray()
            .Select(c => (c.GetProperty("nome").GetString(), c.GetProperty("total").GetInt32())).ToList();
        Assert.Equal(new[] { ("Bruma", 1), ("Vael", 2) }, filtros);
        Assert.Equal(new[] { "Goblin", "Taverneiro" }, Nomes(monstros));
    }

    [Fact]
    public async Task Jogador_so_ve_os_proprios_personagens_na_lista_paginada()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre, "Mesa");
        await EntrarAsync(mestre, jogador, campanhaId);
        await CriarPersonagemAsync(jogador, campanhaId, "Meu");
        await CriarPersonagemAsync(mestre, campanhaId, "Do Mestre");
        await CriarPersonagemAsync(mestre, campanhaId, "Dragão", tipoId: 3);

        var pagina = await GetAsync(jogador, "/Personagem/pagina");

        Assert.Equal(new[] { "Meu" }, Nomes(pagina));
        Assert.Equal(0, pagina.GetProperty("totalMonstros").GetInt32());
    }

    [Fact]
    public async Task Busca_e_filtro_de_campanha_dos_personagens_valem_juntos_e_nao_mudam_as_contagens()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var vael = await CriarCampanhaAsync(mestre, "Vael");
        var bruma = await CriarCampanhaAsync(mestre, "Bruma");
        await CriarPersonagemAsync(mestre, vael, "Thorin", descricao: "anão ferreiro");
        await CriarPersonagemAsync(mestre, vael, "Lyra");
        await CriarPersonagemAsync(mestre, bruma, "Thorvald");

        var porDescricao = await GetAsync(mestre, "/Personagem/pagina?busca=FERREIRO");
        var porNomeNaCampanha = await GetAsync(mestre, $"/Personagem/pagina?busca=thor&campanhaId={bruma}");
        var porCampanha = await GetAsync(mestre, "/Personagem/pagina?busca=bruma");

        Assert.Equal(new[] { "Thorin" }, Nomes(porDescricao));
        Assert.Equal(new[] { "Thorvald" }, Nomes(porNomeNaCampanha));
        Assert.Equal(new[] { "Thorvald" }, Nomes(porCampanha));
        Assert.Equal(3, porNomeNaCampanha.GetProperty("totalHerois").GetInt32());
        Assert.Equal(2, porNomeNaCampanha.GetProperty("porCampanha").GetArrayLength());
    }

    [Fact]
    public async Task Tipo_de_personagem_invalido_e_recusado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.GetAsync("/Personagem/pagina?tipo=dragoes")).StatusCode);
    }

    // ---------- sessões ----------

    [Fact]
    public async Task Sessoes_por_campanha_da_mais_nova_para_a_mais_antiga_com_filtro()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var vael = await CriarCampanhaAsync(mestre, "Vael");
        var bruma = await CriarCampanhaAsync(mestre, "Bruma");
        for (var n = 1; n <= 3; n++) await CriarSessaoAsync(mestre, vael, n);
        await CriarSessaoAsync(mestre, bruma, 1);

        var todas = await GetAsync(mestre, "/Sessao/pagina?tamanho=3");
        var soVael = await GetAsync(mestre, $"/Sessao/pagina?campanhaId={vael}");

        Assert.Equal(4, todas.GetProperty("total").GetInt32());
        var ordem = todas.GetProperty("itens").EnumerateArray()
            .Select(s => (s.GetProperty("campanha").GetProperty("nome").GetString(), s.GetProperty("numeroSessao").GetInt32())).ToList();
        Assert.Equal(new[] { ("Bruma", 1), ("Vael", 3), ("Vael", 2) }, ordem);
        Assert.Equal(3, soVael.GetProperty("total").GetInt32());
        Assert.Equal(2, soVael.GetProperty("porCampanha").GetArrayLength()); // o filtro não some com as outras opções
    }

    // ---------- painel ----------

    [Fact]
    public async Task Painel_traz_totais_destaque_ultima_sessao_agenda_e_herois_numa_resposta()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var antiga = await CriarCampanhaAsync(mestre, "Antiga");
        var nova = await CriarCampanhaAsync(mestre, "Nova");
        await CriarSessaoAsync(mestre, antiga, 1, agendada: DateTime.Today.AddDays(-3)); // agendada no passado: não é próxima
        await CriarSessaoAsync(mestre, nova, 1);
        await CriarSessaoAsync(mestre, nova, 2, agendada: DateTime.Today.AddDays(7));
        await CriarPersonagemAsync(mestre, nova, "Thorin");
        await CriarPersonagemAsync(mestre, nova, "Taverneiro", tipoId: 2);
        await CriarPersonagemAsync(mestre, antiga, "Goblin", tipoId: 3);

        var painel = await GetAsync(mestre, "/Dashboard/resumo");

        Assert.Equal(2, painel.GetProperty("totalCampanhas").GetInt32());
        Assert.Equal(3, painel.GetProperty("totalSessoes").GetInt32());
        Assert.Equal(1, painel.GetProperty("totalHerois").GetInt32());
        Assert.Equal(1, painel.GetProperty("totalNpcs").GetInt32());
        Assert.Equal(1, painel.GetProperty("totalViloes").GetInt32());

        var destaque = painel.GetProperty("destaque");
        Assert.Equal("Nova", destaque.GetProperty("nome").GetString());
        Assert.Equal(2, destaque.GetProperty("totalSessoes").GetInt32());
        Assert.Equal(2, painel.GetProperty("ultimaSessaoDoDestaque").GetProperty("numeroSessao").GetInt32());

        Assert.Equal(new[] { "Nova", "Antiga" }, painel.GetProperty("ultimasCampanhas").EnumerateArray().Select(c => c.GetProperty("nome").GetString()));
        var proxima = Assert.Single(painel.GetProperty("proximasSessoes").EnumerateArray());
        Assert.Equal("Nova", proxima.GetProperty("campanhaNome").GetString());
        Assert.Equal(3, painel.GetProperty("sessoesRecentes").GetArrayLength());
        var heroi = Assert.Single(painel.GetProperty("herois").EnumerateArray());
        Assert.Equal("Thorin", heroi.GetProperty("nome").GetString());
        Assert.Equal("Nova", heroi.GetProperty("campanhaNome").GetString());
    }
}
