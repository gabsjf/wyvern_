using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Wyvern.Application.DTOs.Cena;

namespace Wyvern.Tests;

/// <summary>
/// WYV-25/26: biblioteca de modelos 3D (.glb) da campanha, peças de cenário nos mapas e miniaturas de personagens e tokens.
/// </summary>
public class Modelos3dTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public Modelos3dTests(WyvernApiFactory factory) => _factory = factory;

    // PNG de 1x1 pixel (imagem do mapa)
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>O menor .glb válido: cabeçalho + bloco JSON com o asset.</summary>
    internal static byte[] Glb(string extra = "")
    {
        var json = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"" + extra + "}}");
        var preenchido = (json.Length + 3) / 4 * 4;
        var total = 12 + 8 + preenchido;
        var b = new byte[total];
        Encoding.ASCII.GetBytes("glTF").CopyTo(b, 0);
        BitConverter.GetBytes(2u).CopyTo(b, 4);
        BitConverter.GetBytes((uint)total).CopyTo(b, 8);
        BitConverter.GetBytes((uint)preenchido).CopyTo(b, 12);
        BitConverter.GetBytes(0x4E4F534Au).CopyTo(b, 16);
        json.CopyTo(b, 20);
        for (var i = 20 + json.Length; i < total; i++) b[i] = (byte)' ';
        return b;
    }

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> EnviarAsync(HttpClient c, int campanhaId, string nome, string categoria, byte[]? bytes = null)
    {
        var arquivo = new ByteArrayContent(bytes ?? Glb());
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("model/gltf-binary");
        var form = new MultipartFormDataContent
        {
            { new StringContent(nome), "Nome" },
            { new StringContent(categoria), "Categoria" },
            { arquivo, "Arquivo", "modelo.glb" },
        };
        return c.PostAsync($"/Campanha/{campanhaId}/Modelos", form);
    }

    private static async Task<int> EnviarOkAsync(HttpClient c, int campanhaId, string nome, string categoria)
    {
        var r = await EnviarAsync(c, campanhaId, nome, categoria);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("modeloId").GetInt32();
    }

    private static async Task<int> CriarCenaAtivaAsync(HttpClient mestre, int campanhaId)
    {
        var imagem = new ByteArrayContent(Png);
        imagem.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { new StringContent("Clareira"), "Nome" }, { new StringContent("700"), "Largura" },
            { new StringContent("700"), "Altura" }, { new StringContent("70"), "TamanhoCelula" }, { imagem, "Imagem", "mapa.png" },
        };
        var r = await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas", form);
        r.EnsureSuccessStatusCode();
        var cenaId = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cenaId").GetInt32();
        (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/ativar", null)).EnsureSuccessStatusCode();
        return cenaId;
    }

    private static async Task<JsonElement> BibliotecaAsync(HttpClient c, int campanhaId) =>
        await c.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Modelos");

    // ---------- arquivo ----------

    [Fact]
    public void Glb_valido_precisa_do_cabecalho_certo()
    {
        Assert.True(ArquivoGlb.Valido(Glb()));
        Assert.False(ArquivoGlb.Valido(Png));
        var tamanhoErrado = Glb();
        BitConverter.GetBytes(999u).CopyTo(tamanhoErrado, 8);
        Assert.False(ArquivoGlb.Valido(tamanhoErrado));
        var versao1 = Glb();
        BitConverter.GetBytes(1u).CopyTo(versao1, 4);
        Assert.False(ArquivoGlb.Valido(versao1));
    }

    [Fact]
    public async Task Mestre_sobe_modelos_e_a_mesa_baixa_o_arquivo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        var arvore = await EnviarOkAsync(mestre, campanhaId, "Carvalho", "Cenario");
        await EnviarOkAsync(mestre, campanhaId, "Goblin", "Miniatura");

        var biblioteca = await BibliotecaAsync(jogador, campanhaId);
        var modelos = biblioteca.GetProperty("modelos").EnumerateArray().ToList();
        Assert.Equal(2, modelos.Count);
        Assert.Equal(2L * Glb().Length, biblioteca.GetProperty("espacoUsado").GetInt64());
        Assert.Contains(modelos, m => m.GetProperty("nome").GetString() == "Carvalho" && m.GetProperty("categoria").GetString() == "Cenario");

        var arquivo = await jogador.GetAsync($"/Campanha/{campanhaId}/Modelos/{arvore}/arquivo");
        Assert.Equal(HttpStatusCode.OK, arquivo.StatusCode);
        Assert.Equal("model/gltf-binary", arquivo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Glb(), await arquivo.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Arquivo_que_nao_e_glb_e_categoria_invalida_sao_recusados()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        Assert.Equal(HttpStatusCode.BadRequest, (await EnviarAsync(mestre, campanhaId, "Falso", "Cenario", Png)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await EnviarAsync(mestre, campanhaId, "Outro", "Casa")).StatusCode);
        Assert.Equal(0, (await BibliotecaAsync(mestre, campanhaId)).GetProperty("modelos").GetArrayLength());
    }

    [Fact]
    public async Task Jogador_sobe_miniatura_mas_nao_cenario_e_so_apaga_o_que_subiu()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        var minha = await EnviarOkAsync(jogador, campanhaId, "Minha elfa", "Miniatura");
        Assert.Equal(HttpStatusCode.Forbidden, (await EnviarAsync(jogador, campanhaId, "Casa", "Cenario")).StatusCode);
        var doMestre = await EnviarOkAsync(mestre, campanhaId, "Dragão", "Miniatura");

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Modelos/{doMestre}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Modelos/{minha}")).StatusCode);

        // quem não participa não vê nada
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Modelos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Campanha/{campanhaId}/Modelos/{doMestre}/arquivo")).StatusCode);
    }

    // ---------- peças de cenário no mapa ----------

    [Fact]
    public async Task Mestre_poe_pecas_no_mapa_e_jogador_so_ve_as_visiveis()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);
        var cenaId = await CriarCenaAtivaAsync(mestre, campanhaId);
        var arvore = await EnviarOkAsync(mestre, campanhaId, "Carvalho", "Cenario");
        var mini = await EnviarOkAsync(mestre, campanhaId, "Goblin", "Miniatura");
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/objetos";

        var visivel = await mestre.PostAsJsonAsync(url, new { modeloId = arvore, coluna = 2, linha = 3, tamanho = 2, rotacao = 450 });
        Assert.Equal(HttpStatusCode.OK, visivel.StatusCode);
        var peca = await visivel.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(90, peca.GetProperty("rotacao").GetInt32()); // 450° = 90°
        Assert.Equal("Carvalho", peca.GetProperty("nome").GetString());
        (await mestre.PostAsJsonAsync(url, new { modeloId = arvore, coluna = 5, linha = 5, tamanho = 1, oculto = true })).EnsureSuccessStatusCode();

        // miniatura não é peça de cenário; jogador não mexe no mapa
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsJsonAsync(url, new { modeloId = mini, coluna = 1, linha = 1, tamanho = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync(url, new { modeloId = arvore, coluna = 1, linha = 1, tamanho = 1 })).StatusCode);

        Assert.Equal(2, (await mestre.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());
        Assert.Equal(1, (await jogador.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());

        // a peça não sai do mapa: um 2x2 no canto vai para o último lugar onde cabe
        var id = peca.GetProperty("objetoId").GetInt32();
        var movida = await (await mestre.PutAsJsonAsync($"{url}/{id}", new { modeloId = arvore, coluna = 99, linha = 99, tamanho = 2, rotacao = 0 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(8, movida.GetProperty("coluna").GetInt32());

        // apagar o modelo tira as peças do mapa
        (await mestre.DeleteAsync($"/Campanha/{campanhaId}/Modelos/{arvore}")).EnsureSuccessStatusCode();
        Assert.Equal(0, (await mestre.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());
    }

    [Fact]
    public async Task Editor_3d_guarda_posicao_livre_giro_e_escala_por_eixo()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = await CriarCenaAtivaAsync(mestre, campanhaId); // 10 x 10 quadrados
        var pedra = await EnviarOkAsync(mestre, campanhaId, "Pedra", "Cenario");
        var url = $"/Campanha/{campanhaId}/Cenas/{cenaId}/objetos";

        var criada = await (await mestre.PostAsJsonAsync(url, new
        {
            modeloId = pedra, x = 3.25, y = 0.5, z = 4.75, escalaX = 2.5, escalaY = 1.2, escalaZ = 0.8, rotacao = -30.5, giroX = 15, giroZ = 190,
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3.25, criada.GetProperty("x").GetDouble());
        Assert.Equal(0.5, criada.GetProperty("y").GetDouble());
        Assert.Equal(2.5, criada.GetProperty("escalaX").GetDouble());
        Assert.Equal(0.8, criada.GetProperty("escalaZ").GetDouble());
        Assert.Equal(329.5, criada.GetProperty("rotacao").GetDouble(), 6);
        Assert.Equal(-170, criada.GetProperty("giroZ").GetDouble(), 6); // 190° = -170°
        // o quadrado para o 2D e a névoa: lado maior arredondado (2,5 -> 2), em volta do centro
        Assert.Equal(2, criada.GetProperty("tamanho").GetInt32());
        Assert.Equal(2, criada.GetProperty("coluna").GetInt32());

        // fora do mapa e escala absurda são limitados
        var id = criada.GetProperty("objetoId").GetInt32();
        var limitada = await (await mestre.PutAsJsonAsync($"{url}/{id}", new { modeloId = pedra, x = 99.0, z = -5.0, y = 999.0, escalaX = 500.0, escalaY = 0.0, escalaZ = 1.0 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, limitada.GetProperty("x").GetDouble());
        Assert.Equal(0, limitada.GetProperty("z").GetDouble());
        Assert.Equal(ObjetoCenaRegras.AlturaMaxima, limitada.GetProperty("y").GetDouble());
        Assert.Equal(ObjetoCenaRegras.EscalaMaxima, limitada.GetProperty("escalaX").GetDouble());
        Assert.Equal(ObjetoCenaRegras.EscalaMinima, limitada.GetProperty("escalaY").GetDouble());

        // a forma antiga (o mapa 2D) segue valendo: canto e lado viram centro e escala
        var antiga = await (await mestre.PostAsJsonAsync(url, new { modeloId = pedra, coluna = 1, linha = 2, tamanho = 3, rotacao = 90 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2.5, antiga.GetProperty("x").GetDouble());
        Assert.Equal(3.5, antiga.GetProperty("z").GetDouble());
        Assert.Equal(3, antiga.GetProperty("escalaY").GetDouble());
    }

    [Fact]
    public async Task Duplicar_a_cena_leva_as_pecas()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var cenaId = await CriarCenaAtivaAsync(mestre, campanhaId);
        var mesa = await EnviarOkAsync(mestre, campanhaId, "Mesa", "Cenario");
        (await mestre.PostAsJsonAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/objetos", new { modeloId = mesa, x = 4.3, y = 0.2, z = 4.6, escalaX = 1.5, escalaY = 2, escalaZ = 1, rotacao = 45, giroX = 10 }))
            .EnsureSuccessStatusCode();

        var copia = await (await mestre.PostAsync($"/Campanha/{campanhaId}/Cenas/{cenaId}/duplicar", null)).Content.ReadFromJsonAsync<JsonElement>();

        var pecas = await mestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/Cenas/{copia.GetProperty("cenaId").GetInt32()}/objetos");
        var peca = Assert.Single(pecas.EnumerateArray());
        Assert.Equal(45, peca.GetProperty("rotacao").GetDouble());
        Assert.Equal(4.3, peca.GetProperty("x").GetDouble());
        Assert.Equal(2, peca.GetProperty("escalaY").GetDouble());
        Assert.Equal(10, peca.GetProperty("giroX").GetDouble());
    }

    // ---------- miniaturas ----------

    [Fact]
    public async Task Miniatura_do_personagem_vale_para_os_tokens_e_o_token_pode_ter_a_sua()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);
        var cenaId = await CriarCenaAtivaAsync(mestre, campanhaId);

        var heroi = await (await jogador.PostAsJsonAsync("/Personagem", Fichas.Heroi("Lyra", campanhaId))).Content.ReadFromJsonAsync<JsonElement>();
        var heroiId = heroi.GetProperty("personagemId").GetInt32();
        var elfa = await EnviarOkAsync(jogador, campanhaId, "Elfa arqueira", "Miniatura");
        var arvore = await EnviarOkAsync(mestre, campanhaId, "Carvalho", "Cenario");

        // o dono escolhe a miniatura da própria ficha; peça de cenário não serve
        Assert.Equal(HttpStatusCode.BadRequest, (await jogador.PutAsJsonAsync($"/Personagem/{heroiId}/miniatura", new { modeloId = arvore })).StatusCode);
        var definida = await (await jogador.PutAsJsonAsync($"/Personagem/{heroiId}/miniatura", new { modeloId = elfa })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(elfa, definida.GetProperty("modeloId").GetInt32());

        var tokensUrl = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens";
        var token = await (await mestre.PostAsJsonAsync(tokensUrl, new { nome = "Lyra", coluna = 1, linha = 1, tamanho = 1, cor = "azul", personagemId = heroiId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(elfa, token.GetProperty("miniaturaId").GetInt32());
        Assert.Equal(JsonValueKind.Null, token.GetProperty("modeloId").ValueKind);

        // um token avulso com a sua própria miniatura; cenário não serve como miniatura
        var dragao = await EnviarOkAsync(mestre, campanhaId, "Dragão", "Miniatura");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await mestre.PostAsJsonAsync(tokensUrl, new { nome = "Árvore", coluna = 2, linha = 2, tamanho = 1, cor = "azul", modeloId = arvore })).StatusCode);
        var avulso = await (await mestre.PostAsJsonAsync(tokensUrl, new { nome = "Dragão", coluna = 4, linha = 4, tamanho = 3, cor = "carmim", modeloId = dragao }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(dragao, avulso.GetProperty("miniaturaId").GetInt32());

        // apagar a miniatura: o token volta ao comum, a ficha também
        (await jogador.DeleteAsync($"/Campanha/{campanhaId}/Modelos/{elfa}")).EnsureSuccessStatusCode();
        var tokens = await mestre.GetFromJsonAsync<JsonElement>(tokensUrl);
        var daLyra = tokens.EnumerateArray().Single(t => t.GetProperty("nome").GetString() == "Lyra");
        Assert.Equal(JsonValueKind.Null, daLyra.GetProperty("miniaturaId").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{heroiId}")).GetProperty("modeloId").ValueKind);
    }

    [Fact]
    public async Task Ninguem_poe_miniatura_na_ficha_dos_outros()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (dono, _) = await _factory.NovoUsuarioAsync();
        var (outro, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, dono, campanhaId);
        await EntrarAsync(mestre, outro, campanhaId);
        var heroiId = (await (await dono.PostAsJsonAsync("/Personagem", Fichas.Heroi("Brum", campanhaId))).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("personagemId").GetInt32();
        var mini = await EnviarOkAsync(outro, campanhaId, "Troll", "Miniatura");

        Assert.Equal(HttpStatusCode.NotFound, (await outro.PutAsJsonAsync($"/Personagem/{heroiId}/miniatura", new { modeloId = mini })).StatusCode);
        // o Mestre pode
        Assert.Equal(HttpStatusCode.OK, (await mestre.PutAsJsonAsync($"/Personagem/{heroiId}/miniatura", new { modeloId = mini })).StatusCode);
    }

    [Fact]
    public async Task Modelo_de_outra_campanha_nao_serve()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var a = await CriarCampanhaAsync(mestre);
        var b = await CriarCampanhaAsync(mestre);
        var cenaB = await CriarCenaAtivaAsync(mestre, b);
        var deA = await EnviarOkAsync(mestre, a, "Casa", "Cenario");

        Assert.Equal(HttpStatusCode.BadRequest,
            (await mestre.PostAsJsonAsync($"/Campanha/{b}/Cenas/{cenaB}/objetos", new { modeloId = deA, coluna = 1, linha = 1, tamanho = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{b}/Modelos/{deA}/arquivo")).StatusCode);
    }

    private static Task<HttpResponseMessage> SubstituirAsync(HttpClient c, int campanhaId, int modeloId, byte[] bytes)
    {
        var arquivo = new ByteArrayContent(bytes);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("model/gltf-binary");
        return c.PostAsync($"/Campanha/{campanhaId}/Modelos/{modeloId}/substituir", new MultipartFormDataContent { { arquivo, "Arquivo", "otimizado.glb" } });
    }

    [Fact]
    public async Task Substituir_o_arquivo_cria_um_modelo_novo_e_leva_pecas_tokens_e_fichas()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);
        var cenaId = await CriarCenaAtivaAsync(mestre, campanhaId);

        var arvore = await EnviarOkAsync(mestre, campanhaId, "Carvalho", "Cenario");
        var elfa = await EnviarOkAsync(jogador, campanhaId, "Elfa", "Miniatura");
        var heroiId = (await (await jogador.PostAsJsonAsync("/Personagem", Fichas.Heroi("Lyra", campanhaId))).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("personagemId").GetInt32();
        (await jogador.PutAsJsonAsync($"/Personagem/{heroiId}/miniatura", new { modeloId = elfa })).EnsureSuccessStatusCode();
        var objetosUrl = $"/Campanha/{campanhaId}/Cenas/{cenaId}/objetos";
        var tokensUrl = $"/Campanha/{campanhaId}/Cenas/{cenaId}/tokens";
        (await mestre.PostAsJsonAsync(objetosUrl, new { modeloId = arvore, coluna = 2, linha = 2, tamanho = 2, rotacao = 30 })).EnsureSuccessStatusCode();
        (await mestre.PostAsJsonAsync(tokensUrl, new { nome = "Lyra", coluna = 1, linha = 1, tamanho = 1, cor = "azul", personagemId = heroiId })).EnsureSuccessStatusCode();
        (await mestre.PostAsJsonAsync(tokensUrl, new { nome = "Sentinela", coluna = 5, linha = 5, tamanho = 1, cor = "azul", modeloId = elfa })).EnsureSuccessStatusCode();
        (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/Modelos/{elfa}", new { nome = "Elfa", escala = "Impressao" })).EnsureSuccessStatusCode();

        // só quem subiu (ou o Mestre); arquivo inválido não serve
        var leve = Glb(",\"generator\":\"otimizado\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await SubstituirAsync(jogador, campanhaId, arvore, leve)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SubstituirAsync(jogador, campanhaId, elfa, Png)).StatusCode);

        var r = await SubstituirAsync(jogador, campanhaId, elfa, leve);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var nova = await r.Content.ReadFromJsonAsync<JsonElement>();
        var novaId = nova.GetProperty("modeloId").GetInt32();
        Assert.NotEqual(elfa, novaId);
        Assert.Equal("Elfa", nova.GetProperty("nome").GetString());
        Assert.Equal("Impressao", nova.GetProperty("escala").GetString());
        Assert.Equal(leve.Length, nova.GetProperty("tamanho").GetInt64());

        // o antigo sumiu; ficha e os dois tokens usam o novo, com os bytes novos
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{campanhaId}/Modelos/{elfa}/arquivo")).StatusCode);
        Assert.Equal(leve, await (await mestre.GetAsync($"/Campanha/{campanhaId}/Modelos/{novaId}/arquivo")).Content.ReadAsByteArrayAsync());
        Assert.Equal(novaId, (await jogador.GetFromJsonAsync<JsonElement>($"/Personagem/{heroiId}")).GetProperty("modeloId").GetInt32());
        var tokens = (await mestre.GetFromJsonAsync<JsonElement>(tokensUrl)).EnumerateArray().ToList();
        Assert.All(tokens, t => Assert.Equal(novaId, t.GetProperty("miniaturaId").GetInt32()));

        // a peça do mapa continua no lugar, agora com o modelo novo; o espaço usado conta só o novo
        var arvoreNova = (await (await SubstituirAsync(mestre, campanhaId, arvore, leve)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("modeloId").GetInt32();
        var peca = Assert.Single((await mestre.GetFromJsonAsync<JsonElement>(objetosUrl)).EnumerateArray());
        Assert.Equal(arvoreNova, peca.GetProperty("modeloId").GetInt32());
        Assert.Equal(30, peca.GetProperty("rotacao").GetInt32());
        var biblioteca = await BibliotecaAsync(mestre, campanhaId);
        Assert.Equal(2, biblioteca.GetProperty("modelos").GetArrayLength());
        Assert.Equal(2L * leve.Length, biblioteca.GetProperty("espacoUsado").GetInt64());
    }

    [Fact]
    public async Task Escala_do_modelo_vem_no_envio_e_pode_ser_trocada_por_quem_subiu()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        // sem escala: Auto; com escala de impressão (o .stl convertido no navegador manda assim)
        var auto = await EnviarOkAsync(mestre, campanhaId, "Ogro", "Miniatura");
        var arquivo = new ByteArrayContent(Glb());
        var form = new MultipartFormDataContent { { new StringContent("Orc"), "Nome" }, { new StringContent("Miniatura"), "Categoria" }, { new StringContent("Impressao"), "Escala" }, { arquivo, "Arquivo", "orc.glb" } };
        var impressa = await (await jogador.PostAsync($"/Campanha/{campanhaId}/Modelos", form)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Impressao", impressa.GetProperty("escala").GetString());

        var lista = (await BibliotecaAsync(mestre, campanhaId)).GetProperty("modelos").EnumerateArray().ToList();
        Assert.Equal("Auto", lista.Single(m => m.GetProperty("modeloId").GetInt32() == auto).GetProperty("escala").GetString());

        var url = $"/Campanha/{campanhaId}/Modelos/{auto}";
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PutAsJsonAsync(url, new { nome = "Ogro", escala = "Gigante" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PutAsJsonAsync(url, new { nome = "Meu", escala = "Real" })).StatusCode);
        var editado = await (await mestre.PutAsJsonAsync(url, new { nome = "  Ogro das cavernas ", escala = "Real" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Real", editado.GetProperty("escala").GetString());
        Assert.Equal("Ogro das cavernas", editado.GetProperty("nome").GetString());
    }
}
