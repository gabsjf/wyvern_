using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class DuplicarPersonagemTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public DuplicarPersonagemTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre, string nome = "Mesa")
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5.5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task<int> CriarMonstroAsync(HttpClient mestre, int campanhaId)
    {
        var resposta = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Goblin", campanhaId, tipoId = 3, descricao = "Pequeno e mau",
            atributo = new { forca = 8, destreza = 14, constituicao = 10, inteligencia = 10, sabedoria = 8, carisma = 8 },
            personagemCombate = new { vidaMaxima = 7, vidaAtual = 7, classeArmadura = 15, condicoes = "", deslocamento = "9m" },
            personagemNpc = new { categoriaUso = "Inimigo", tamanho = "Pequeno", tipoCriatura = "Humanoide", nivelDesafio = "1/4", xpConcedido = 50, formulaDadoVida = "2d6" },
            personagemAtaques = new[] { new { nome = "Cimitarra", alcance = "1,5m", bonusAcerto = 4, dano = "1d6+2", tipoDano = "Cortante", propriedades = "Acuidade" } },
            personagemAcoesPadrao = new[] { new { nome = "Ataque", descricao = "Corpo a corpo", alcance = "1,5m", bonusAcerto = 4, dano = "1d6+2", tipoDano = "Cortante", propriedades = "", atributoBase = "Destreza", proficiente = true, tipoAcao = "Ataque" } },
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    [Fact]
    public async Task Copia_na_mesma_campanha_leva_a_ficha_inteira_com_ids_novos()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var original = await CriarMonstroAsync(mestre, campanhaId);

        var resposta = await mestre.PostAsJsonAsync($"/Personagem/{original}/duplicar", new { });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var copia = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(original, copia.GetProperty("personagemId").GetInt32());
        Assert.Equal("Goblin (cópia)", copia.GetProperty("nome").GetString());
        Assert.Equal(3, copia.GetProperty("tipoId").GetInt32());
        Assert.Equal(campanhaId, copia.GetProperty("campanhaId").GetInt32());
        Assert.Equal(14, copia.GetProperty("atributo").GetProperty("destreza").GetInt32());
        Assert.Equal(15, copia.GetProperty("personagemCombate").GetProperty("classeArmadura").GetInt32());
        Assert.Equal("2d6", copia.GetProperty("personagemNpc").GetProperty("formulaDadoVida").GetString());
        Assert.Equal("Cimitarra", copia.GetProperty("personagemAtaques")[0].GetProperty("nome").GetString());
        Assert.Equal("Ataque", copia.GetProperty("personagemAcoesPadrao")[0].GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Copia_e_independente_do_original()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var original = await CriarMonstroAsync(mestre, campanhaId);
        var copiaId = (await (await mestre.PostAsJsonAsync($"/Personagem/{original}/duplicar", new { nome = "Goblin 2" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        (await mestre.PutAsJsonAsync($"/Personagem/{copiaId}/estado", new { vidaAtual = 1 })).EnsureSuccessStatusCode();

        var doOriginal = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{original}")).GetProperty("personagemCombate");
        Assert.Equal(7, doOriginal.GetProperty("vidaAtual").GetInt32());
        var daCopia = await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{copiaId}");
        Assert.Equal("Goblin 2", daCopia.GetProperty("nome").GetString());
        Assert.Equal(1, daCopia.GetProperty("personagemCombate").GetProperty("vidaAtual").GetInt32());
    }

    [Fact]
    public async Task Copia_para_outra_campanha_do_mesmo_mestre_mantem_o_nome()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var primeira = await CriarCampanhaAsync(mestre, "Primeira");
        var segunda = await CriarCampanhaAsync(mestre, "Segunda");
        var original = await CriarMonstroAsync(mestre, primeira);

        var copia = await (await mestre.PostAsJsonAsync($"/Personagem/{original}/duplicar", new { campanhaId = segunda }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(segunda, copia.GetProperty("campanhaId").GetInt32());
        Assert.Equal("Goblin", copia.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Nao_copia_para_campanha_em_que_o_usuario_nao_e_mestre()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (outroMestre, _) = await _factory.NovoUsuarioAsync();
        var minha = await CriarCampanhaAsync(mestre, "Minha");
        var alheia = await CriarCampanhaAsync(outroMestre, "Alheia");
        var original = await CriarMonstroAsync(mestre, minha);

        Assert.Equal(HttpStatusCode.Forbidden, (await mestre.PostAsJsonAsync($"/Personagem/{original}/duplicar", new { campanhaId = alheia })).StatusCode);
    }

    [Fact]
    public async Task Quem_nao_enxerga_o_original_nao_o_copia()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var original = await CriarMonstroAsync(mestre, campanhaId);

        Assert.Equal(HttpStatusCode.NotFound, (await estranho.PostAsJsonAsync($"/Personagem/{original}/duplicar", new { })).StatusCode);
    }
}
