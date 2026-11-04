using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Wyvern.Tests;

public class CenaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public CenaTests(WyvernApiFactory factory) => _factory = factory;

    // PNG de 1x1 pixel
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

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

    private static MultipartFormDataContent Formulario(string nome, byte[]? imagem, int largura = 700, int altura = 700, int celula = 70)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(nome), "Nome" },
            { new StringContent(largura.ToString()), "Largura" },
            { new StringContent(altura.ToString()), "Altura" },
            { new StringContent(celula.ToString()), "TamanhoCelula" },
        };
        if (imagem != null)
        {
            var arquivo = new ByteArrayContent(imagem);
            arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(arquivo, "Imagem", "mapa.png");
        }
        return form;
    }

    private static async Task<JsonElement> CriarCenaAsync(HttpClient mestre, int campanhaId, string nome = "Taverna")
    {
        var resposta = await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas", Formulario(nome, Png));
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static object Token(string nome = "Brum", int coluna = 1, int linha = 1, bool oculto = false, int? personagemId = null, int? controladoPorId = null) =>
        new { nome, coluna, linha, tamanho = 1, cor = "azul", inimigo = false, oculto, personagemId, controladoPorId };

    private static async Task<JsonElement> CriarTokenAsync(HttpClient mestre, int campanhaId, int cenaId, object token)
    {
        var resposta = await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens", token);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HubConnection> ConectarAsync(string token)
    {
        var conexao = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/cenahub"), o =>
            {
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await conexao.StartAsync();
        return conexao;
    }

    // ---------- cenas ----------

    [Fact]
    public async Task Primeira_cena_ja_fica_em_jogo_e_a_segunda_nao()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var primeira = await CriarCenaAsync(mestre, campanhaId, "Taverna");
        var segunda = await CriarCenaAsync(mestre, campanhaId, "Masmorra");

        Assert.True(primeira.GetProperty("ativa").GetBoolean());
        Assert.False(segunda.GetProperty("ativa").GetBoolean());
        Assert.Equal(2, (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas")).GetArrayLength());
    }

    [Fact]
    public async Task Jogador_ve_so_a_cena_em_jogo_e_a_troca_vale_para_ele()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        await CriarCenaAsync(mestre, campanhaId, "Taverna");
        var masmorra = await CriarCenaAsync(mestre, campanhaId, "Masmorra");

        var antes = (await jogador.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas")).EnumerateArray().Single();
        Assert.Equal("Taverna", antes.GetProperty("nome").GetString());

        var masmorraId = masmorra.GetProperty("cenaId").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.GetAsync($"/Campanha/{campanhaId}/Cenas/{masmorraId}/imagem")).StatusCode);

        (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{masmorraId}/ativar", null)).EnsureSuccessStatusCode();

        var depois = (await jogador.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas")).EnumerateArray().Single();
        Assert.Equal("Masmorra", depois.GetProperty("nome").GetString());
        Assert.Equal(HttpStatusCode.OK, (await jogador.GetAsync($"/Campanha/{campanhaId}/Cenas/{masmorraId}/imagem")).StatusCode);
    }

    [Fact]
    public async Task So_o_mestre_mexe_nos_mapas_e_estranho_nao_sabe_que_a_campanha_existe()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cena = await CriarCenaAsync(mestre, campanhaId);
        var cenaId = cena.GetProperty("cenaId").GetInt32();

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsync($"/Campanha/{campanhaId}/Cenas", Formulario("X", Png))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await jogador.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens", Token())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Cenas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/imagem")).StatusCode);
    }

    [Fact]
    public async Task Arquivo_que_nao_e_imagem_e_recusado_mesmo_com_tipo_de_imagem()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var resposta = await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas", Formulario("Falsa", "<script>alert(1)</script>"u8.ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Empty((await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas")).EnumerateArray());
    }

    [Fact]
    public async Task Imagem_volta_igual_e_o_navegador_reaproveita_pela_etag()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();

        var resposta = await mestre.GetAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/imagem");
        Assert.Equal("image/png", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await resposta.Content.ReadAsByteArrayAsync());

        var requisicao = new HttpRequestMessage(HttpMethod.Get, $"/Campanha/{campanhaId}/Cenas/{cenaId}/imagem");
        requisicao.Headers.TryAddWithoutValidation("If-None-Match", resposta.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.NotModified, (await mestre.SendAsync(requisicao)).StatusCode);
    }

    [Fact]
    public async Task Excluir_a_cena_leva_os_tokens_junto()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        await CriarTokenAsync(mestre, campanhaId, cenaId, Token());

        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}")).StatusCode);

        Assert.Empty((await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).StatusCode);
    }

    // ---------- tokens ----------

    [Fact]
    public async Task Token_oculto_so_aparece_para_o_mestre_e_fica_dentro_do_mapa()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10 quadrados

        await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Visível"));
        var emboscada = await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Emboscada", coluna: 900, linha: 900, oculto: true));

        Assert.Equal(9, emboscada.GetProperty("coluna").GetInt32());
        Assert.Equal(9, emboscada.GetProperty("linha").GetInt32());
        var doJogador = (await jogador.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).EnumerateArray().ToList();
        Assert.Equal("Visível", Assert.Single(doJogador).GetProperty("nome").GetString());
        Assert.Equal(2, (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).GetArrayLength());
    }

    [Fact]
    public async Task Token_nao_aceita_cor_fora_da_paleta_nem_personagem_de_outra_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var outra = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var personagem = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Estrangeiro", campanhaId = outra, tipoId = 1,
            personagemPlayer = new { classe = "Mago", nivel = 1, raca = "Elfo" },
            personagemCombate = new { vidaMaxima = 8, vidaAtual = 8, dadoVidaMaximo = "1d6" },
        });
        var personagemId = (await personagem.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var corRuim = await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens",
            new { nome = "X", coluna = 0, linha = 0, tamanho = 1, cor = "#ff0000" });
        var deOutra = await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens", Token(personagemId: personagemId));

        Assert.Equal(HttpStatusCode.BadRequest, corRuim.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deOutra.StatusCode);
    }

    [Fact]
    public async Task Token_guarda_o_raio_de_visao_e_recusa_valor_absurdo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens";

        var tocha = await mestre.PostAsJsonAsync(url, new { nome = "Foco", coluna = 2, linha = 2, tamanho = 1, cor = "ouro", visao = 30 });
        var absurdo = await mestre.PostAsJsonAsync(url, new { nome = "Farol", coluna = 2, linha = 2, tamanho = 1, cor = "ouro", visao = 5000 });

        Assert.Equal(30, (await tocha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("visao").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, absurdo.StatusCode);
        // o jogador recebe o raio, para a névoa dele abrir ao redor do foco
        var doJogador = (await jogador.GetFromJsonAsync<JsonElement>(url)).EnumerateArray().Single();
        Assert.Equal(30, doJogador.GetProperty("visao").GetInt32());
    }

    [Fact]
    public async Task Tokens_do_combate_entram_no_mapa_uma_vez_so()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var sessaoId = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();

        var semCombate = await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate", null);
        Assert.Equal(HttpStatusCode.BadRequest, semCombate.StatusCode);

        (await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId, campanhaId,
            participantes = new object[]
            {
                new { nomeNPC = "Brum", iniciativa = 15, vidaAtual = 20, vidaMaxima = 31, classeArmadura = 15, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Goblin", iniciativa = 10, vidaAtual = 7, vidaMaxima = 7, classeArmadura = 13, isInimigo = true, condicoes = "" },
            },
        })).EnsureSuccessStatusCode();

        var primeira = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate", null)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().ToList();
        var segunda = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate", null)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().ToList();

        Assert.Equal(2, primeira.Count);
        Assert.Empty(segunda);
        var goblin = primeira.Single(t => t.GetProperty("nome").GetString() == "Goblin");
        Assert.True(goblin.GetProperty("inimigo").GetBoolean());
        Assert.Equal("carmim", goblin.GetProperty("cor").GetString());
        Assert.NotEqual(primeira[0].GetProperty("coluna").GetInt32(), primeira[1].GetProperty("coluna").GetInt32()); // cada um no seu quadrado
    }

    [Fact]
    public async Task Personagem_que_ja_esta_no_mapa_e_ligado_ao_combate_em_vez_de_duplicado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var sessaoId = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        var personagem = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Thorin", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Guerreiro", nivel = 5, raca = "Humano" },
            personagemCombate = new { vidaMaxima = 40, vidaAtual = 40, dadoVidaMaximo = "5d10" },
        });
        var personagemId = (await personagem.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
        var antes = await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Thorin", coluna: 4, linha: 4, personagemId: personagemId));
        (await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId, campanhaId,
            participantes = new object[]
            {
                new { personagemId, iniciativa = 15, vidaAtual = 40, vidaMaxima = 40, classeArmadura = 16, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Goblin", iniciativa = 10, vidaAtual = 7, vidaMaxima = 7, classeArmadura = 13, isInimigo = true, condicoes = "" },
            },
        })).EnsureSuccessStatusCode();

        var resposta = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate", null)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().ToList();

        var tokens = (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).EnumerateArray().ToList();
        Assert.Equal(2, tokens.Count); // Thorin não foi duplicado
        var thorin = tokens.Single(t => t.GetProperty("nome").GetString() == "Thorin");
        Assert.Equal(antes.GetProperty("tokenId").GetInt32(), thorin.GetProperty("tokenId").GetInt32());
        Assert.Equal(4, thorin.GetProperty("coluna").GetInt32()); // continua onde o Mestre o pôs
        Assert.NotEqual(JsonValueKind.Null, thorin.GetProperty("participanteId").ValueKind);
        Assert.Equal(2, resposta.Count); // o ligado e o novo são devolvidos, para a tela atualizar
    }

    // ---------- mover (hub) ----------

    [Fact]
    public async Task Jogador_move_so_o_que_e_dele_e_todos_veem_o_movimento()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var (outro, sessaoOutro) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        await EntrarNaCampanhaAsync(mestre, outro, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var meu = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Meu", controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();
        var alheio = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Alheio"))).GetProperty("tokenId").GetInt32();

        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        await using var hubOutro = await ConectarAsync(sessaoOutro.Token);
        var recebido = new TaskCompletionSource<JsonElement>();
        hubOutro.On<JsonElement>("TokenMovido", m => recebido.TrySetResult(m));
        foreach (var hub in new[] { hubMestre, hubJogador, hubOutro }) await hub.InvokeAsync("EntrarNaCena", campanhaId);

        await hubJogador.InvokeAsync("MoverToken", campanhaId, meu, 4, 5);
        var movimento = await recebido.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(4, movimento.GetProperty("coluna").GetInt32());
        Assert.Equal(5, movimento.GetProperty("linha").GetInt32());

        await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, alheio, 2, 2));
        await Assert.ThrowsAsync<HubException>(() => hubOutro.InvokeAsync("MoverToken", campanhaId, meu, 2, 2));
        await hubMestre.InvokeAsync("MoverToken", campanhaId, alheio, 7, 7); // o Mestre move qualquer um

        var tokens = (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).EnumerateArray().ToList();
        Assert.Equal(4, tokens.Single(t => t.GetProperty("tokenId").GetInt32() == meu).GetProperty("coluna").GetInt32());
        Assert.Equal(7, tokens.Single(t => t.GetProperty("tokenId").GetInt32() == alheio).GetProperty("coluna").GetInt32());
    }

    [Fact]
    public async Task Movimento_que_passa_do_mapa_e_ajustado_e_token_oculto_nao_se_move_nem_vaza()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        var visivel = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Meu", controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();
        var escondido = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Escondido", oculto: true, controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();

        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await hubJogador.InvokeAsync("EntrarNaCena", campanhaId);
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);
        var vazou = false;
        hubJogador.On<JsonElement>("TokenMovido", m => { if (m.GetProperty("tokenId").GetInt32() == escondido) vazou = true; });
        var mestreViu = new TaskCompletionSource<JsonElement>();
        hubMestre.On<JsonElement>("TokenMovido", m => { if (m.GetProperty("tokenId").GetInt32() == escondido) mestreViu.TrySetResult(m); });

        await hubJogador.InvokeAsync("MoverToken", campanhaId, visivel, 99, -3);
        await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, escondido, 3, 3));
        await hubMestre.InvokeAsync("MoverToken", campanhaId, escondido, 3, 3);
        await mestreViu.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200);

        var token = (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).EnumerateArray()
            .Single(t => t.GetProperty("tokenId").GetInt32() == visivel);
        Assert.Equal(9, token.GetProperty("coluna").GetInt32());
        Assert.Equal(0, token.GetProperty("linha").GetInt32());
        Assert.False(vazou);
    }

    [Fact]
    public async Task Fora_da_cena_em_jogo_o_jogador_nao_move_nada()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        await CriarCenaAsync(mestre, campanhaId, "Taverna");
        var masmorraId = (await CriarCenaAsync(mestre, campanhaId, "Masmorra")).GetProperty("cenaId").GetInt32(); // não está em jogo
        var token = (await CriarTokenAsync(mestre, campanhaId, masmorraId, Token(controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();

        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, token, 2, 2));
    }

    // ---------- névoa de guerra e ping ----------

    private static bool Revelado(JsonElement neblina, int coluna, int linha)
    {
        var bytes = Convert.FromBase64String(neblina.GetProperty("revelado").GetString()!);
        var i = linha * neblina.GetProperty("colunas").GetInt32() + coluna;
        return (bytes[i >> 3] & (1 << (i & 7))) != 0;
    }

    [Fact]
    public async Task Nevoa_nasce_coberta_o_mestre_revela_e_cobre_e_todos_veem_na_hora()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/neblina";

        var ligada = await (await mestre.PutAsJsonAsync(url, new { ativa = true })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(ligada.GetProperty("ativa").GetBoolean());
        Assert.False(Revelado(ligada, 0, 0)); // começa tudo coberto

        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        var avisado = new TaskCompletionSource<JsonElement>();
        hubJogador.On<JsonElement>("NeblinaAlterada", n => avisado.TrySetResult(n));
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);
        await hubJogador.InvokeAsync("EntrarNaCena", campanhaId);

        await hubMestre.InvokeAsync("AlterarNeblina", campanhaId, cenaId, 2, 3, 3, 2, true);
        var aviso = await avisado.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, aviso.GetProperty("coluna").GetInt32());

        var vista = await jogador.GetFromJsonAsync<JsonElement>(url);
        Assert.True(Revelado(vista, 2, 3));
        Assert.True(Revelado(vista, 4, 4));
        Assert.False(Revelado(vista, 5, 3));
        Assert.False(Revelado(vista, 1, 3));

        await hubMestre.InvokeAsync("AlterarNeblina", campanhaId, cenaId, 3, 3, 1, 1, false); // cobre de novo um quadrado
        Assert.False(Revelado(await jogador.GetFromJsonAsync<JsonElement>(url), 3, 3));

        // retângulo que passa do mapa é cortado, sem erro
        await hubMestre.InvokeAsync("AlterarNeblina", campanhaId, cenaId, 8, 8, 50, 50, true);
        Assert.True(Revelado(await jogador.GetFromJsonAsync<JsonElement>(url), 9, 9));
    }

    [Fact]
    public async Task Jogador_nao_mexe_na_nevoa_e_revelar_tudo_funciona()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/neblina";

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PutAsJsonAsync(url, new { ativa = false })).StatusCode);
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("AlterarNeblina", campanhaId, cenaId, 0, 0, 3, 3, true));
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PutAsJsonAsync(url, new { ativa = true, tudo = "talvez" })).StatusCode);

        var tudo = await (await mestre.PutAsJsonAsync(url, new { ativa = true, tudo = "revelar" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(Revelado(tudo, 0, 0));
        Assert.True(Revelado(tudo, 9, 9));
        var coberto = await (await mestre.PutAsJsonAsync(url, new { ativa = true, tudo = "cobrir" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(Revelado(coberto, 9, 9));
    }

    [Fact]
    public async Task Mudar_o_tamanho_do_quadrado_cobre_a_nevoa_de_novo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/neblina";
        await mestre.PutAsJsonAsync(url, new { ativa = true, tudo = "revelar" });

        (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}", new { nome = "Taverna", tamanhoCelula = 100 })).EnsureSuccessStatusCode();

        var neblina = await mestre.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(7, neblina.GetProperty("colunas").GetInt32()); // 700 / 100
        Assert.False(Revelado(neblina, 0, 0));
        Assert.True(neblina.GetProperty("ativa").GetBoolean()); // continua ligada
    }

    [Fact]
    public async Task Ping_chega_a_todos_com_o_autor_do_token_e_estranho_nao_pinga()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var (_, sessaoEstranho) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();

        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        await using var hubEstranho = await ConectarAsync(sessaoEstranho.Token);
        var recebido = new TaskCompletionSource<JsonElement>();
        hubMestre.On<JsonElement>("Ping", p => recebido.TrySetResult(p));
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);
        await hubJogador.InvokeAsync("EntrarNaCena", campanhaId);

        await hubJogador.InvokeAsync("Ping", campanhaId, cenaId, 350.5, 420.0);

        var ping = await recebido.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(sessaoJogador.UsuarioId, ping.GetProperty("usuarioId").GetInt32());
        Assert.Equal(350.5, ping.GetProperty("x").GetDouble());
        await Assert.ThrowsAsync<HubException>(() => hubEstranho.InvokeAsync("Ping", campanhaId, cenaId, 1.0, 1.0));
    }

    // ---------- paredes, retratos, duplicar e vez de agir ----------

    [Fact]
    public async Task Parede_e_porta_so_o_mestre_cria_e_o_jogador_le_na_cena_em_jogo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/paredes";

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync(url, new { x1 = 0, y1 = 0, x2 = 3, y2 = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync(url, new { x1 = 2, y1 = 2, x2 = 2, y2 = 2 })).StatusCode); // sem comprimento
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync(url, new { x1 = 0, y1 = 0, x2 = 11, y2 = 0 })).StatusCode); // além do mapa

        (await mestre.PostAsJsonAsync(url, new { x1 = 0, y1 = 3, x2 = 4, y2 = 3 })).EnsureSuccessStatusCode();
        var porta = await (await mestre.PostAsJsonAsync(url, new { x1 = 4, y1 = 3, x2 = 5, y2 = 3, porta = true })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(porta.GetProperty("aberta").GetBoolean());

        var aberta = await mestre.PutAsJsonAsync($"{url}/{porta.GetProperty("paredeId").GetInt32()}", new { x1 = 4, y1 = 3, x2 = 5, y2 = 3, porta = true, aberta = true });
        Assert.True((await aberta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("aberta").GetBoolean());
        Assert.Equal(2, (await jogador.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());

        var apagar = await mestre.DeleteAsync($"{url}/{porta.GetProperty("paredeId").GetInt32()}");
        Assert.Equal(HttpStatusCode.NoContent, apagar.StatusCode);
        Assert.Equal(1, (await mestre.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());
    }

    private static MultipartFormDataContent Retrato(byte[] bytes)
    {
        var arquivo = new ByteArrayContent(bytes);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { arquivo, "Imagem", "retrato.png" } };
    }

    [Fact]
    public async Task Retrato_do_token_sobe_volta_igual_e_token_escondido_nao_vaza()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var visivel = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Brum"))).GetProperty("tokenId").GetInt32();
        var escondido = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Sombra", oculto: true))).GetProperty("tokenId").GetInt32();
        var baseUrl = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens";

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PutAsync($"{baseUrl}/{visivel}/imagem", Retrato(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PutAsync($"{baseUrl}/{visivel}/imagem", Retrato("não sou imagem"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PutAsync($"{baseUrl}/{visivel}/imagem", Retrato(new byte[600 * 1024]))).StatusCode);

        var enviado = await (await mestre.PutAsync($"{baseUrl}/{visivel}/imagem", Retrato(Png))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, enviado.GetProperty("imagemVersao").GetInt32());
        (await mestre.PutAsync($"{baseUrl}/{escondido}/imagem", Retrato(Png))).EnsureSuccessStatusCode();

        var resposta = await jogador.GetAsync($"{baseUrl}/{visivel}/imagem");
        Assert.Equal(Png, await resposta.Content.ReadAsByteArrayAsync());
        var cache = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{visivel}/imagem");
        cache.Headers.TryAddWithoutValidation("If-None-Match", resposta.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.NotModified, (await jogador.SendAsync(cache)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.GetAsync($"{baseUrl}/{escondido}/imagem")).StatusCode);

        var removido = await (await mestre.DeleteAsync($"{baseUrl}/{visivel}/imagem")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, removido.GetProperty("imagemVersao").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.GetAsync($"{baseUrl}/{visivel}/imagem")).StatusCode);
    }

    [Fact]
    public async Task Duplicar_copia_imagem_tokens_retratos_e_paredes_sem_entrar_em_jogo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId, "Taverna")).GetProperty("cenaId").GetInt32();
        var tokenId = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Brum", 3, 4))).GetProperty("tokenId").GetInt32();
        (await mestre.PutAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/{tokenId}/imagem", Retrato(Png))).EnsureSuccessStatusCode();
        (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/paredes", new { x1 = 0, y1 = 2, x2 = 5, y2 = 2 })).EnsureSuccessStatusCode();
        await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/neblina", new { ativa = true, tudo = "revelar" });

        var copia = await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/duplicar", null)).Content.ReadFromJsonAsync<JsonElement>();

        var copiaId = copia.GetProperty("cenaId").GetInt32();
        Assert.NotEqual(cenaId, copiaId);
        Assert.Equal("Taverna (cópia)", copia.GetProperty("nome").GetString());
        Assert.False(copia.GetProperty("ativa").GetBoolean());
        Assert.True(copia.GetProperty("neblinaAtiva").GetBoolean());
        var tokens = (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{copiaId}/tokens")).EnumerateArray().ToList();
        var token = Assert.Single(tokens);
        Assert.Equal(3, token.GetProperty("coluna").GetInt32());
        Assert.NotEqual(tokenId, token.GetProperty("tokenId").GetInt32());
        var retrato = await mestre.GetAsync($"/Campanha/{campanhaId}/Cenas/{copiaId}/tokens/{token.GetProperty("tokenId").GetInt32()}/imagem");
        Assert.Equal(Png, await retrato.Content.ReadAsByteArrayAsync());
        Assert.Equal(1, (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{copiaId}/paredes")).GetArrayLength());
        Assert.Equal(Png, await (await mestre.GetAsync($"/Campanha/{campanhaId}/Cenas/{copiaId}/imagem")).Content.ReadAsByteArrayAsync());
        Assert.True(Revelado(await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{copiaId}/neblina"), 9, 9));
    }

    [Fact]
    public async Task Cena_so_pode_ser_ligada_a_sessao_da_mesma_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var outra = await CriarCampanhaAsync(mestre);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        async Task<int> NovaSessaoAsync(int campanha) =>
            (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S", dataSessao = DateTime.Today, obs = "", campanhaId = campanha }))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        var daMesa = await NovaSessaoAsync(campanhaId);
        var deOutra = await NovaSessaoAsync(outra);
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}";

        var ligada = await mestre.PutAsJsonAsync(url, new { nome = "Taverna", tamanhoCelula = 70, sessaoId = daMesa, soNoTurno = true });
        var estranha = await mestre.PutAsJsonAsync(url, new { nome = "Taverna", tamanhoCelula = 70, sessaoId = deOutra });

        var corpo = await ligada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(daMesa, corpo.GetProperty("sessaoId").GetInt32());
        Assert.True(corpo.GetProperty("soNoTurno").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, estranha.StatusCode);
    }

    [Fact]
    public async Task Com_a_regra_do_turno_o_jogador_so_move_o_token_de_quem_esta_na_vez()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var sessaoId = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        var inicio = await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId, campanhaId,
            participantes = new object[]
            {
                new { nomeNPC = "Primeiro", iniciativa = 15, vidaAtual = 10, vidaMaxima = 10, classeArmadura = 12, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Segundo", iniciativa = 10, vidaAtual = 10, vidaMaxima = 10, classeArmadura = 12, isInimigo = false, condicoes = "" },
            },
        });
        var combateId = (await inicio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();
        var doCombate = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate", null)).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();

        // os dois tokens passam a ser do jogador
        var ids = new Dictionary<string, int>();
        foreach (var t in doCombate)
        {
            var nome = t.GetProperty("nome").GetString()!;
            ids[nome] = t.GetProperty("tokenId").GetInt32();
            (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/{ids[nome]}", new
            {
                nome, coluna = t.GetProperty("coluna").GetInt32(), linha = t.GetProperty("linha").GetInt32(), tamanho = 1, cor = "azul",
                participanteId = t.GetProperty("participanteId").GetInt32(), controladoPorId = sessaoJogador.UsuarioId,
            })).EnsureSuccessStatusCode();
        }
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        // sem a regra, qualquer um dos dois se move
        await hub.InvokeAsync("MoverToken", campanhaId, ids["Segundo"], 6, 6);

        (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}", new { nome = "Taverna", tamanhoCelula = 70, soNoTurno = true })).EnsureSuccessStatusCode();

        await hub.InvokeAsync("MoverToken", campanhaId, ids["Primeiro"], 5, 5); // é a vez dele
        var erro = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, ids["Segundo"], 7, 7));
        Assert.Contains("vez", erro.Message);

        (await mestre.PostAsync($"/Combate/{combateId}/next-turn", null)).EnsureSuccessStatusCode();

        await hub.InvokeAsync("MoverToken", campanhaId, ids["Segundo"], 7, 7);
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, ids["Primeiro"], 1, 1));
    }

    [Fact]
    public async Task A_vez_e_a_do_combate_do_proprio_token_mesmo_com_outro_combate_ativo_na_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        async Task<int> NovaSessaoAsync(int numero) =>
            (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = numero, nome = "S" + numero, dataSessao = DateTime.Today, obs = "", campanhaId }))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        async Task<int> IniciarAsync(int sessaoId, string npc, int iniciativa)
        {
            var r = await mestre.PostAsJsonAsync("/Combate/start", new
            {
                sessaoId, campanhaId,
                participantes = new object[] { new { nomeNPC = npc, iniciativa, vidaAtual = 10, vidaMaxima = 10, classeArmadura = 12, isInimigo = false, condicoes = "" } },
            });
            r.EnsureSuccessStatusCode();
            return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();
        }

        // um combate antigo que ficou aberto em outra sessão, com um combatente à frente da fila
        var antigo = await IniciarAsync(await NovaSessaoAsync(1), "Antigo", 20);
        var sessaoAtual = await NovaSessaoAsync(2);
        var combateId = await IniciarAsync(sessaoAtual, "Lyra", 15);

        var doCombate = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate?sessaoId={sessaoAtual}", null)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().ToList();
        var lyra = doCombate.Single(t => t.GetProperty("nome").GetString() == "Lyra");
        var tokenId = lyra.GetProperty("tokenId").GetInt32();
        (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/{tokenId}", new
        {
            nome = "Lyra", coluna = 2, linha = 2, tamanho = 1, cor = "azul",
            participanteId = lyra.GetProperty("participanteId").GetInt32(), controladoPorId = sessaoJogador.UsuarioId,
        })).EnsureSuccessStatusCode();
        (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}", new { nome = "Taverna", tamanhoCelula = 70, soNoTurno = true })).EnsureSuccessStatusCode();

        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        // é a vez da Lyra no combate dela: o combate antigo não pode atrapalhar
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 6);

        // acabou o combate dela: o token fica livre, em vez de preso a um combate que já não existe
        (await mestre.PostAsync($"/Combate/{combateId}/end", null)).EnsureSuccessStatusCode();
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 7, 7);
        Assert.True(antigo > 0);
    }

    [Fact]
    public async Task Parede_janela_e_porta_fechada_barram_o_jogador_mas_nao_o_mestre()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        var tokenId = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Lyra", 2, 2, controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();
        var urlParedes = $"/Campanha/{campanhaId}/Cenas/{cenaId}/paredes";
        var parede = await (await mestre.PostAsJsonAsync(urlParedes, new { x1 = 4, y1 = 0, x2 = 4, y2 = 10 })).Content.ReadFromJsonAsync<JsonElement>();
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await hubJogador.InvokeAsync("EntrarNaCena", campanhaId);
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);

        // a parede vertical em x = 4 vai de cima a baixo: não há caminho de x = 2 para x = 6
        var erro = await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 2));
        Assert.Contains("parede", erro.Message);
        await hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 3, 7); // do lado de cá: livre
        await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 7)); // a parede fecha a passagem inteira
        await hubMestre.InvokeAsync("MoverToken", campanhaId, tokenId, 2, 2);  // o Mestre atravessa

        // janela: barra o movimento do mesmo jeito
        await mestre.DeleteAsync($"{urlParedes}/{parede.GetProperty("paredeId").GetInt32()}");
        var janela = await (await mestre.PostAsJsonAsync(urlParedes, new { x1 = 4, y1 = 0, x2 = 4, y2 = 10, janela = true })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(janela.GetProperty("janela").GetBoolean());
        await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 2));

        // porta: fechada barra, aberta deixa passar
        await mestre.DeleteAsync($"{urlParedes}/{janela.GetProperty("paredeId").GetInt32()}");
        var porta = await (await mestre.PostAsJsonAsync(urlParedes, new { x1 = 4, y1 = 0, x2 = 4, y2 = 10, porta = true })).Content.ReadFromJsonAsync<JsonElement>();
        await Assert.ThrowsAsync<HubException>(() => hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 2));
        await mestre.PutAsJsonAsync($"{urlParedes}/{porta.GetProperty("paredeId").GetInt32()}", new { x1 = 4, y1 = 0, x2 = 4, y2 = 10, porta = true, aberta = true });
        await hubJogador.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 2);
    }

    private static object Marcador(string titulo = "Armadilha de dardos", int coluna = 3, int linha = 3, bool visivel = false) =>
        new { coluna, linha, icone = "armadilha", titulo, nota = "CD 15 para achar", visivel };

    [Fact]
    public async Task Marcador_escondido_so_o_mestre_ve_e_revelar_ou_esconder_chega_aos_jogadores()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/marcadores";

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync(url, Marcador())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync(url, new { coluna = 1, linha = 1, icone = "nada", titulo = "X" })).StatusCode);
        var escondido = await (await mestre.PostAsJsonAsync(url, Marcador())).Content.ReadFromJsonAsync<JsonElement>();
        var id = escondido.GetProperty("marcadorId").GetInt32();
        Assert.Empty((await jogador.GetFromJsonAsync<JsonElement>(url)).EnumerateArray());
        Assert.Single((await mestre.GetFromJsonAsync<JsonElement>(url)).EnumerateArray());

        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);
        var revelado = new TaskCompletionSource<JsonElement>();
        var removido = new TaskCompletionSource<JsonElement>();
        hub.On<JsonElement>("MarcadorAtualizado", m => revelado.TrySetResult(m));
        hub.On<JsonElement>("MarcadorRemovido", m => removido.TrySetResult(m));

        await mestre.PutAsJsonAsync($"{url}/{id}", Marcador(visivel: true));
        var aviso = await revelado.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Armadilha de dardos", aviso.GetProperty("titulo").GetString());
        Assert.Single((await jogador.GetFromJsonAsync<JsonElement>(url)).EnumerateArray());

        await mestre.PutAsJsonAsync($"{url}/{id}", Marcador(visivel: false));
        await removido.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty((await jogador.GetFromJsonAsync<JsonElement>(url)).EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"{url}/{id}")).StatusCode);
    }

    private static object Traco(int pontos = 5, string cor = "azul") =>
        new { cor, espessura = 4, pontos = Enumerable.Range(0, pontos).Select(i => new[] { 100.0 + i * 10, 200.5 }).ToArray() };

    [Fact]
    public async Task Todos_desenham_cada_um_apaga_o_seu_e_o_mestre_apaga_qualquer_um_e_limpa()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var (outro, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        await EntrarNaCampanhaAsync(mestre, outro, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tracos";

        var doJogador = await (await jogador.PostAsJsonAsync(url, Traco())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, doJogador.GetProperty("pontos").GetArrayLength());
        Assert.Equal(200.5, doJogador.GetProperty("pontos")[0][1].GetDouble());
        var doMestre = await (await mestre.PostAsJsonAsync(url, Traco(3, "ouro"))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, (await outro.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());

        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync(url, Traco(1))).StatusCode);   // um ponto não é traço
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync(url, Traco(5, "#ff0000"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PostAsJsonAsync(url, Traco(601))).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await outro.DeleteAsync($"{url}/{doJogador.GetProperty("tracoId").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"{url}/{doMestre.GetProperty("tracoId").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await jogador.DeleteAsync($"{url}/{doJogador.GetProperty("tracoId").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync(url)).StatusCode); // limpar tudo é do Mestre

        await jogador.PostAsJsonAsync(url, Traco());
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync(url)).StatusCode);
        Assert.Empty((await mestre.GetFromJsonAsync<JsonElement>(url)).EnumerateArray());
    }

    [Fact]
    public async Task Regua_compartilhada_chega_aos_outros_e_nao_volta_para_quem_mede()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32();
        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await using var hubJogador = await ConectarAsync(sessaoJogador.Token);
        var recebida = new TaskCompletionSource<JsonElement>();
        var eco = false;
        hubMestre.On<JsonElement>("ReguaCompartilhada", r => recebida.TrySetResult(r));
        hubJogador.On<JsonElement>("ReguaCompartilhada", _ => eco = true);
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);
        await hubJogador.InvokeAsync("EntrarNaCena", campanhaId);

        await hubJogador.InvokeAsync("Regua", campanhaId, cenaId, 1, 1, 5, 4, true);

        var r = await recebida.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(sessaoJogador.UsuarioId, r.GetProperty("usuarioId").GetInt32());
        Assert.Equal(5, r.GetProperty("c2").GetInt32());
        Assert.True(r.GetProperty("ativa").GetBoolean());
        await Task.Delay(200);
        Assert.False(eco);
    }

    [Fact]
    public async Task Nao_da_para_escapar_na_diagonal_pelo_vertice_de_uma_sala_fechada()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        var tokenId = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Lyra", 3, 3, controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/paredes";
        // uma sala fechada de (2,2) a (4,4); o canto de baixo à direita é o vértice (4, 4)
        foreach (var (x1, y1, x2, y2) in new[] { (2, 2, 4, 2), (4, 2, 4, 4), (4, 4, 2, 4), (2, 4, 2, 2) })
            await mestre.PostAsJsonAsync(url, new { x1, y1, x2, y2 });
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        // de (3, 3) para (4, 4) o passo na diagonal passa exatamente pelo vértice onde duas paredes se encontram
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 4, 4));
        // e não há outro caminho: a sala está fechada
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 6));
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 2, 2); // dentro da sala, livre
    }

    [Fact]
    public async Task Token_de_tamanho_par_nao_pode_parar_sobre_a_parede_para_atravessar_em_dois_passos()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10
        // 2x2 em (2, 2): o centro fica em (3, 3), sobre as linhas da grade
        var tokenId = (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens",
            new { nome = "Ogro", coluna = 2, linha = 2, tamanho = 2, cor = "laranja", controladoPorId = sessaoJogador.UsuarioId }))
            .Content.ReadFromJsonAsync<JsonElement>().Result.GetProperty("tokenId").GetInt32();
        await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/paredes", new { x1 = 4, y1 = 0, x2 = 4, y2 = 10 });
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        // o token 2x2 não pode ficar "dividido" pela parede (com o centro sobre ela)
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 3, 2));
        // passo 2 direto: atravessar
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 4, 2));
        // chegar bem perto, sem tocar, é livre
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 2, 4);
    }

    // ---------- deslocamento (da ficha) ----------

    [Theory]
    [InlineData("9m", 9.0)]
    [InlineData("9 m", 9.0)]
    [InlineData("9,5m", 9.5)]
    [InlineData("12 metros", 12.0)]
    [InlineData("30 ft", 9.0)]
    [InlineData("30 pés", 9.0)]
    [InlineData("30", 9.0)]            // sem unidade e acima de 20: pés
    [InlineData("9", 9.0)]             // sem unidade e até 20: metros
    [InlineData("9m, voo 18m", 9.0)]   // só vale o primeiro
    public void Deslocamento_da_ficha_e_lido_em_metros(string texto, double esperado) =>
        Assert.Equal(esperado, Wyvern.Application.DTOs.Cena.MovimentoRegras.ParseMetros(texto)!.Value, 6);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("veloz")]
    public void Deslocamento_ilegivel_nao_inventa_limite(string? texto) =>
        Assert.Null(Wyvern.Application.DTOs.Cena.MovimentoRegras.ParseMetros(texto));

    [Fact]
    public void Quadrados_cabem_em_metros_para_baixo()
    {
        Assert.Equal(6, Wyvern.Application.DTOs.Cena.MovimentoRegras.Quadrados(9, 1.5));
        Assert.Equal(6, Wyvern.Application.DTOs.Cena.MovimentoRegras.Quadrados(10, 1.5)); // 6,66 → 6
        Assert.Equal(3, Wyvern.Application.DTOs.Cena.MovimentoRegras.Quadrados(9, 3));
        Assert.Equal(0, Wyvern.Application.DTOs.Cena.MovimentoRegras.Quadrados(1, 1.5));
    }

    private static Wyvern.Domain.Entities.Cena CenaDeTeste(int colunas = 10, int linhas = 10) =>
        new() { Largura = colunas * 70, Altura = linhas * 70, TamanhoCelula = 70 };

    private static Wyvern.Domain.Entities.ParedeCena Muro(int x1, int y1, int x2, int y2, bool porta = false, bool aberta = false) =>
        new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Porta = porta, Aberta = aberta };

    [Fact]
    public void Alcance_em_campo_aberto_e_um_quadrado_com_a_diagonal_valendo_um()
    {
        var alcance = Wyvern.Application.DTOs.Cena.MovimentoRegras.Alcance(CenaDeTeste(), 1, 5, 5, [], limite: 2);

        Assert.Equal(25, alcance.Count); // 5x5 ao redor de (5, 5)
        Assert.Equal(2, alcance[(7, 7)]);
        Assert.Equal(1, alcance[(6, 4)]);
        Assert.False(alcance.ContainsKey((8, 5)));
    }

    [Fact]
    public void Alcance_contorna_a_parede_em_vez_de_atravessar_e_porta_aberta_encurta()
    {
        var cena = CenaDeTeste();
        // parede de x = 5 que vai do topo até y = 8: o jeito é dar a volta por baixo
        var contorno = Wyvern.Application.DTOs.Cena.MovimentoRegras.Custo(cena, 1, 4, 2, 6, 2, [Muro(5, 0, 5, 8)]);
        var livre = Wyvern.Application.DTOs.Cena.MovimentoRegras.Custo(cena, 1, 4, 2, 6, 2, []);
        Assert.Equal(2, livre);
        Assert.True(contorno > 8, $"o contorno deveria passar de 8 quadrados, deu {contorno}");

        // a mesma barreira, mas com uma porta fechada no meio e depois aberta
        var fechada = Wyvern.Application.DTOs.Cena.MovimentoRegras.Custo(cena, 1, 4, 2, 6, 2, [Muro(5, 0, 5, 2), Muro(5, 2, 5, 3, porta: true), Muro(5, 3, 5, 10)]);
        var aberta = Wyvern.Application.DTOs.Cena.MovimentoRegras.Custo(cena, 1, 4, 2, 6, 2, [Muro(5, 0, 5, 2), Muro(5, 2, 5, 3, porta: true, aberta: true), Muro(5, 3, 5, 10)]);
        Assert.Null(fechada); // sem passagem nenhuma
        Assert.Equal(2, aberta);
    }

    [Fact]
    public void Token_grande_precisa_caber_e_nao_pode_ser_dividido_pela_parede()
    {
        var cena = CenaDeTeste();
        // um corredor de 1 quadrado de largura (entre x = 3 e x = 5? não: x = 4 e x = 5) não deixa passar um token 2x2
        var corredor = new[] { Muro(4, 0, 4, 10), Muro(5, 0, 5, 10) };
        var um = Wyvern.Application.DTOs.Cena.MovimentoRegras.Alcance(cena, 1, 4, 0, corredor, 20);
        var dois = Wyvern.Application.DTOs.Cena.MovimentoRegras.Alcance(cena, 2, 4, 0, corredor, 20);

        Assert.True(um.ContainsKey((4, 9)));  // o token pequeno desce o corredor inteiro
        Assert.Single(dois);                  // o 2x2 nem sai do lugar
    }

    private async Task<(HttpClient mestre, HttpClient jogador, WyvernApiFactory.Sessao sessaoJogador, int campanhaId, int cenaId, int tokenId, WyvernApiFactory.Sessao sessaoMestre, int combateId)> MesaDeCombateAsync(string deslocamento)
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, jogador, campanhaId);
        var cenaId = (await CriarCenaAsync(mestre, campanhaId)).GetProperty("cenaId").GetInt32(); // 10x10, 1,5 m por quadrado
        var sessaoId = (await (await mestre.PostAsJsonAsync("/Sessao", new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        var personagem = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Lyra", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Guerreiro", nivel = 5, raca = "Humano" },
            personagemCombate = new { vidaMaxima = 40, vidaAtual = 40, dadoVidaMaximo = "5d10", deslocamento },
        });
        var personagemId = (await personagem.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
        var tokenId = (await CriarTokenAsync(mestre, campanhaId, cenaId, Token("Lyra", 0, 0, personagemId: personagemId, controladoPorId: sessaoJogador.UsuarioId))).GetProperty("tokenId").GetInt32();
        var inicio = await mestre.PostAsJsonAsync("/Combate/start", new
        {
            sessaoId, campanhaId,
            participantes = new object[]
            {
                new { personagemId, iniciativa = 15, vidaAtual = 40, vidaMaxima = 40, classeArmadura = 16, isInimigo = false, condicoes = "" },
                new { nomeNPC = "Goblin", iniciativa = 10, vidaAtual = 7, vidaMaxima = 7, classeArmadura = 13, isInimigo = true, condicoes = "" },
            },
        });
        var combateId = (await inicio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();
        (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/do-combate?sessaoId={sessaoId}", null)).EnsureSuccessStatusCode();
        return (mestre, jogador, sessaoJogador, campanhaId, cenaId, tokenId, sessaoMestre, combateId);
    }

    [Fact]
    public async Task Token_mostra_o_deslocamento_da_ficha_e_o_manual_vale_mais()
    {
        var (mestre, _, _, campanhaId, cenaId, tokenId, _, _) = await MesaDeCombateAsync("9m");
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens";

        var token = (await mestre.GetFromJsonAsync<JsonElement>(url)).EnumerateArray().Single(t => t.GetProperty("tokenId").GetInt32() == tokenId);
        Assert.Equal(9.0, token.GetProperty("deslocamentoMetros").GetDouble());
        Assert.Equal(9.0, token.GetProperty("deslocamentoDaFicha").GetDouble());
        Assert.Equal(JsonValueKind.Null, token.GetProperty("deslocamentoManual").ValueKind);

        var editado = await mestre.PutAsJsonAsync($"{url}/{tokenId}", new
        {
            nome = "Lyra", coluna = 0, linha = 0, tamanho = 1, cor = "azul", deslocamentoMetros = 4.5,
            personagemId = token.GetProperty("personagemId").GetInt32(), participanteId = token.GetProperty("participanteId").GetInt32(),
        });
        var depois = await editado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4.5, depois.GetProperty("deslocamentoMetros").GetDouble());
        Assert.Equal(9.0, depois.GetProperty("deslocamentoDaFicha").GetDouble()); // a ficha continua à mostra
    }

    [Fact]
    public async Task No_combate_o_jogador_anda_so_o_deslocamento_da_ficha_por_rodada()
    {
        var (mestre, _, sessaoJogador, campanhaId, cenaId, tokenId, _, combateId) = await MesaDeCombateAsync("9m"); // 6 quadrados
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 4, 0); // gasta 4, restam 2
        var erro = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 7, 0)); // +3
        Assert.Contains("insuficiente", erro.Message);
        Assert.Contains("2 quadrado", erro.Message);
        Assert.Contains("3 m", erro.Message);

        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 6, 0); // +2: esgota
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 7, 0));

        // a rodada seguinte devolve o movimento
        (await mestre.PostAsync($"/Combate/{combateId}/next-turn", null)).EnsureSuccessStatusCode();
        (await mestre.PostAsync($"/Combate/{combateId}/next-turn", null)).EnsureSuccessStatusCode();
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 1, 5);
    }

    [Fact]
    public async Task O_mestre_nao_tem_limite_mas_o_que_anda_conta_para_a_rodada()
    {
        var (mestre, _, sessaoJogador, campanhaId, cenaId, tokenId, sessaoMestre, _) = await MesaDeCombateAsync("9m");
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);
        await using var hubMestre = await ConectarAsync(sessaoMestre.Token);
        await hubMestre.InvokeAsync("EntrarNaCena", campanhaId);

        await hubMestre.InvokeAsync("MoverToken", campanhaId, tokenId, 8, 0); // 8 quadrados: passa dos 6, mas é o Mestre

        var token = (await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens")).EnumerateArray()
            .Single(t => t.GetProperty("tokenId").GetInt32() == tokenId);
        Assert.Equal(8, token.GetProperty("movimentoGasto").GetInt32());
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 9, 0)); // a jogadora já não tem o que andar

        // "restaurar movimento" devolve tudo
        await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens/{tokenId}", new
        {
            nome = "Lyra", coluna = 8, linha = 0, tamanho = 1, cor = "azul", restaurarMovimento = true, controladoPorId = sessaoJogador.UsuarioId,
            personagemId = token.GetProperty("personagemId").GetInt32(), participanteId = token.GetProperty("participanteId").GetInt32(),
        });
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 9, 0);
    }

    [Fact]
    public async Task A_parede_que_obriga_a_dar_a_volta_gasta_o_caminho_todo_e_o_metro_por_quadrado_da_cena_vale()
    {
        var (mestre, _, sessaoJogador, campanhaId, cenaId, tokenId, _, _) = await MesaDeCombateAsync("9m"); // 6 quadrados
        var urlCena = $"/Campanha/{campanhaId}/Cenas/{cenaId}";
        // uma parede de x = 3 do topo até y = 8: da (0,0) até (5,0) o jeito é descer e voltar
        await mestre.PostAsJsonAsync($"{urlCena}/paredes", new { x1 = 3, y1 = 0, x2 = 3, y2 = 8 });
        await using var hub = await ConectarAsync(sessaoJogador.Token);
        await hub.InvokeAsync("EntrarNaCena", campanhaId);

        var erro = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 5, 0));
        Assert.Contains("insuficiente", erro.Message); // há caminho, mas passa dos 6 quadrados

        // com 3 m por quadrado o mesmo 9 m vale só 3 quadrados
        (await mestre.PutAsJsonAsync(urlCena, new { nome = "Taverna", tamanhoCelula = 70, metrosPorQuadrado = 3.0, limitarDeslocamento = true })).EnsureSuccessStatusCode();
        var erro2 = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MoverToken", campanhaId, tokenId, 4, 0));
        Assert.Contains("3 quadrado", erro2.Message);

        // e desligar o limite libera (a parede continua valendo)
        (await mestre.PutAsJsonAsync(urlCena, new { nome = "Taverna", tamanhoCelula = 70, metrosPorQuadrado = 1.5, limitarDeslocamento = false })).EnsureSuccessStatusCode();
        await hub.InvokeAsync("MoverToken", campanhaId, tokenId, 5, 0);
    }

    [Fact]
    public async Task Estranho_nao_entra_no_mapa_da_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (_, sessaoEstranho) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        await using var hub = await ConectarAsync(sessaoEstranho.Token);

        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("EntrarNaCena", campanhaId));
    }
}
