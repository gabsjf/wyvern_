using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

public class CronicaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public CronicaTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarNaCampanhaAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static object Evento(string titulo = "A ponte caiu", string tipo = "Marco", bool soMestre = false, int? ordem = null, int? sessaoId = null) =>
        new { titulo, descricao = "Texto **em markdown**", tipo, soMestre, ordem, sessaoId, rotuloData = "3 de Mirtul" };

    private static async Task<JsonElement> CriarEventoAsync(HttpClient cliente, int campanhaId, object evento)
    {
        var resposta = await cliente.PostAsJsonAsync($"/Campanha/{campanhaId}/Eventos", evento);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<List<JsonElement>> ListarAsync(HttpClient cliente, int campanhaId) =>
        (await cliente.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Eventos")).EnumerateArray().ToList();

    // ---------- leitura e criação ----------

    [Fact]
    public async Task Membro_cria_evento_e_a_lista_devolve_com_autor_e_origem()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);

        var criado = await CriarEventoAsync(jogador, campanhaId, Evento("Achei um mapa"));

        Assert.Equal("Achei um mapa", criado.GetProperty("titulo").GetString());
        Assert.Equal("Manual", criado.GetProperty("origem").GetString());
        Assert.Equal(sessaoJogador.UsuarioId, criado.GetProperty("autorId").GetInt32());
        Assert.Equal("Todos", criado.GetProperty("visibilidade").GetString());
        Assert.Single(await ListarAsync(mestre, campanhaId), e => e.GetProperty("titulo").GetString() == "Achei um mapa");
    }

    [Fact]
    public async Task Evento_sem_posicao_vai_para_o_fim_da_cronologia()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var a = await CriarEventoAsync(mestre, campanhaId, Evento("A", ordem: 5));
        var b = await CriarEventoAsync(mestre, campanhaId, Evento("B"));

        Assert.Equal(6, b.GetProperty("ordem").GetInt32());
        Assert.Equal(new[] { "A", "B" }, (await ListarAsync(mestre, campanhaId)).Select(e => e.GetProperty("titulo").GetString()));
    }

    // ---------- spoilers ----------

    [Fact]
    public async Task Evento_so_do_mestre_nao_aparece_para_jogadores()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);

        var segredo = await CriarEventoAsync(mestre, campanhaId, Evento("O vilão é o rei", soMestre: true));
        await CriarEventoAsync(mestre, campanhaId, Evento("A festa"));

        Assert.Equal(2, (await ListarAsync(mestre, campanhaId)).Count);
        var doJogador = await ListarAsync(jogador, campanhaId);
        Assert.Single(doJogador);
        Assert.Equal("A festa", doJogador[0].GetProperty("titulo").GetString());

        // e o jogador nem consegue mexer no evento escondido: para ele não existe
        var id = segredo.GetProperty("eventoId").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.PutAsJsonAsync($"/Campanha/{campanhaId}/Eventos/{id}", Evento())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Eventos/{id}")).StatusCode);
    }

    [Fact]
    public async Task Jogador_nao_consegue_esconder_evento_do_mestre()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);

        var criado = await CriarEventoAsync(jogador, campanhaId, Evento("Tentei esconder", soMestre: true));

        Assert.Equal("Todos", criado.GetProperty("visibilidade").GetString());
    }

    // ---------- edição e exclusão ----------

    [Fact]
    public async Task Jogador_edita_e_exclui_so_o_que_escreveu()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (rafa, _) = await _factory.NovoUsuarioAsync();
        var (bia, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, rafa, campanhaId);
        await EntrarNaCampanhaAsync(mestre, bia, campanhaId);

        var doRafa = (await CriarEventoAsync(rafa, campanhaId, Evento("Do Rafa"))).GetProperty("eventoId").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await rafa.PutAsJsonAsync($"/Campanha/{campanhaId}/Eventos/{doRafa}", Evento("Do Rafa, editado"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bia.PutAsJsonAsync($"/Campanha/{campanhaId}/Eventos/{doRafa}", Evento("invadido"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bia.DeleteAsync($"/Campanha/{campanhaId}/Eventos/{doRafa}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await rafa.DeleteAsync($"/Campanha/{campanhaId}/Eventos/{doRafa}")).StatusCode);
    }

    [Fact]
    public async Task Mestre_edita_e_exclui_qualquer_evento_e_pode_esconder_depois()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (rafa, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, rafa, campanhaId);
        var id = (await CriarEventoAsync(rafa, campanhaId, Evento("Do Rafa"))).GetProperty("eventoId").GetInt32();

        var resposta = await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Eventos/{id}", Evento("Do Rafa, escondido", soMestre: true));

        Assert.Equal("Mestre", (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("visibilidade").GetString());
        Assert.Empty(await ListarAsync(rafa, campanhaId));
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/Eventos/{id}")).StatusCode);
    }

    // ---------- quem não participa ----------

    [Fact]
    public async Task Quem_nao_participa_da_campanha_nao_ve_nem_escreve()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var id = (await CriarEventoAsync(mestre, campanhaId, Evento())).GetProperty("eventoId").GetInt32();

        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Eventos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.PostAsJsonAsync($"/Campanha/{campanhaId}/Eventos", Evento())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.PutAsJsonAsync($"/Campanha/{campanhaId}/Eventos/{id}", Evento())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.DeleteAsync($"/Campanha/{campanhaId}/Eventos/{id}")).StatusCode);
    }

    // ---------- validação ----------

    [Fact]
    public async Task Evento_invalido_e_recusado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var outraCampanha = await CriarCampanhaAsync(mestre);
        var sessaoOutra = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S", dataSessao = DateTime.Today, obs = "", campanhaId = outraCampanha }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Eventos", Evento(titulo: "   "))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Eventos", Evento(tipo: "Inventado"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Eventos", Evento(sessaoId: sessaoOutra))).StatusCode);
    }

    // ---------- eventos automáticos ----------

    [Fact]
    public async Task Criar_uma_sessao_abre_um_capitulo_na_cronica()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var sessao = await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 3, nome = "O cerco", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var evento = Assert.Single(await ListarAsync(mestre, campanhaId));
        Assert.Equal("Sessão 3: O cerco", evento.GetProperty("titulo").GetString());
        Assert.Equal("Automatico", evento.GetProperty("origem").GetString());
        Assert.Equal(3, evento.GetProperty("sessaoNumero").GetInt32());
        Assert.Equal(sessao.GetProperty("sessaoId").GetInt32(), evento.GetProperty("sessaoId").GetInt32());
    }

    [Fact]
    public async Task Descanso_fica_registrado_na_cronica()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var personagem = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Thorin", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Guerreiro", nivel = 5, raca = "Anão" },
            personagemCombate = new { vidaMaxima = 40, vidaAtual = 10, dadoVidaMaximo = "5d10" },
        });
        var id = (await personagem.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        (await mestre.PostAsJsonAsync($"/Personagem/{id}/descanso", new
        {
            tipo = "Longo", vidaAtual = 40, vidaTemporaria = 0, dadoVidaGasto = 0, deathSaveSucessos = 0, deathSaveFalhas = 0, restaurarEspacosMagia = false,
        })).EnsureSuccessStatusCode();

        var evento = Assert.Single(await ListarAsync(mestre, campanhaId));
        Assert.Equal("Thorin fez um descanso longo", evento.GetProperty("titulo").GetString());
        Assert.Equal("Descanso", evento.GetProperty("tipo").GetString());
        Assert.Contains("10 → 40", evento.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task Encerrar_o_combate_registra_quem_caiu_na_cronica()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var sessaoId = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();

        var inicio = await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId, campanhaId,
            participantes = new object[]
            {
                new { nomeNPC = "Brum", iniciativa = 15, vidaAtual = 20, vidaMaxima = 31, classeArmadura = 15, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Lyra", iniciativa = 12, vidaAtual = 0, vidaMaxima = 34, classeArmadura = 14, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Goblin", iniciativa = 10, vidaAtual = 0, vidaMaxima = 7, classeArmadura = 13, isInimigo = true, condicoes = "" },
                new { nomeNPC = "Cultista", iniciativa = 9, vidaAtual = 20, vidaMaxima = 45, classeArmadura = 13, isInimigo = true, condicoes = "" },
            },
        });
        inicio.EnsureSuccessStatusCode();
        var combateId = (await inicio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await mestre.PostAsync($"/Combate/{combateId}/end", null)).StatusCode);

        var evento = (await ListarAsync(mestre, campanhaId)).Single(e => e.GetProperty("tipo").GetString() == "Combate");
        Assert.Equal("Combate: 1 inimigo derrotado em 1 rodada", evento.GetProperty("titulo").GetString());
        var texto = evento.GetProperty("descricao").GetString()!;
        Assert.Contains("**Derrotados:** Goblin", texto);
        Assert.Contains("**Inimigos que restaram:** Cultista", texto);
        Assert.Contains("**Heróis caídos:** Lyra", texto);
        Assert.Contains("**Heróis de pé:** Brum", texto);
        Assert.Equal(sessaoId, evento.GetProperty("sessaoId").GetInt32());
    }

    // ---------- texto do resumo de combate ----------

    private static CombateParticipante P(string nome, bool inimigo, int vida) => new() { NomeNPC = nome, IsInimigo = inimigo, VidaAtual = vida };

    [Fact]
    public void Resumo_de_combate_sem_derrotados_tem_titulo_neutro_e_pluraliza()
    {
        var combate = new Combate { RodadaAtual = 3, Participantes = [P("Brum", false, 10), P("Goblin", true, 5)] };

        var (titulo, descricao) = ResumoDeCombate.Criar(combate);

        Assert.Equal("Combate encerrado em 3 rodadas", titulo);
        Assert.Contains("**Inimigos que restaram:** Goblin", descricao);
        Assert.DoesNotContain("Derrotados", descricao);
    }

    [Fact]
    public void Resumo_de_combate_pluraliza_varios_inimigos_derrotados()
    {
        var combate = new Combate { RodadaAtual = 1, Participantes = [P("A", true, 0), P("B", true, 0)] };

        Assert.Equal("Combate: 2 inimigos derrotados em 1 rodada", ResumoDeCombate.Criar(combate).Titulo);
    }
}
