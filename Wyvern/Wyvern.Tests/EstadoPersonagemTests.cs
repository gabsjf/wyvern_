using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class EstadoPersonagemTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public EstadoPersonagemTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    /// <summary>40 PV (30 atuais), 3 espaços de 1º nível (todos disponíveis) e 2 de 2º.</summary>
    private static async Task<int> CriarPersonagemAsync(HttpClient cliente, int campanhaId)
    {
        var resposta = await cliente.PostAsJsonAsync("/Personagem", new
        {
            nome = "Lyra", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Mago", nivel = 5, raca = "Elfo", antecedente = "Soldado", subclasse = "Campeão" },
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 40, vidaAtual = 30, condicoes = "Caído", deslocamento = "9m" },
            personagemConjuracao = new { atributoConjuracao = "Inteligencia", slotsNivel1Max = 3, slotsNivel1Atual = 3, slotsNivel2Max = 2, slotsNivel2Atual = 2 },
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
    }

    private static async Task<JsonElement> LerAsync(HttpClient cliente, int id) =>
        await cliente.GetFromJsonAsync<JsonElement>($"/Personagem/{id}");

    [Fact]
    public async Task Condicoes_sao_salvas_e_devolvidas_pela_ficha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var id = await CriarPersonagemAsync(mestre, await CriarCampanhaAsync(mestre));

        Assert.Equal("Caído", (await LerAsync(mestre, id)).GetProperty("personagemCombate").GetProperty("condicoes").GetString());
    }

    [Fact]
    public async Task Mudanca_parcial_altera_so_o_que_foi_enviado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var id = await CriarPersonagemAsync(mestre, await CriarCampanhaAsync(mestre));

        var resposta = await mestre.PutAsJsonAsync($"/Personagem/{id}/estado", new { condicoes = "Envenenado, Agarrado", inspiracaoHeroica = true });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var combate = (await LerAsync(mestre, id)).GetProperty("personagemCombate");
        Assert.Equal("Envenenado, Agarrado", combate.GetProperty("condicoes").GetString());
        Assert.True(combate.GetProperty("inspiracaoHeroica").GetBoolean());
        Assert.Equal(30, combate.GetProperty("vidaAtual").GetInt32()); // intocado
    }

    [Fact]
    public async Task Gastar_espacos_de_magia_e_guardado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var id = await CriarPersonagemAsync(mestre, await CriarCampanhaAsync(mestre));

        (await mestre.PutAsJsonAsync($"/Personagem/{id}/estado", new { espacosAtuais = new[] { 1, 2, 0, 0, 0, 0, 0, 0, 0 } })).EnsureSuccessStatusCode();

        var conjuracao = (await LerAsync(mestre, id)).GetProperty("personagemConjuracao");
        Assert.Equal(1, conjuracao.GetProperty("slotsNivel1Atual").GetInt32());
        Assert.Equal(2, conjuracao.GetProperty("slotsNivel2Atual").GetInt32());
    }

    [Theory]
    [InlineData("{\"vidaAtual\":41}", "acima da vida máxima")]
    [InlineData("{\"vidaAtual\":-1}", "PV negativos")]
    [InlineData("{\"vidaTemporaria\":-3}", "PV temporários negativos")]
    [InlineData("{\"deathSaveFalhas\":4}", "salvaguarda acima de 3")]
    [InlineData("{\"espacosAtuais\":[4,0,0,0,0,0,0,0,0]}", "mais espaços que o máximo")]
    [InlineData("{\"espacosAtuais\":[1,2]}", "lista de tamanho errado")]
    [InlineData("{\"espacosAtuais\":[0,0,0,0,0,0,0,0,-1]}", "espaço negativo")]
    public async Task Valores_fora_dos_limites_sao_recusados(string corpo, string motivo)
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var id = await CriarPersonagemAsync(mestre, await CriarCampanhaAsync(mestre));

        var resposta = await mestre.PutAsync($"/Personagem/{id}/estado", new StringContent(corpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.True(resposta.StatusCode == HttpStatusCode.BadRequest, $"esperava 400 para {motivo}, veio {(int)resposta.StatusCode}");
    }

    [Fact]
    public async Task Quem_nao_e_dono_nem_mestre_nao_altera_a_ficha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var id = await CriarPersonagemAsync(mestre, await CriarCampanhaAsync(mestre));

        Assert.Equal(HttpStatusCode.NotFound, (await estranho.PutAsJsonAsync($"/Personagem/{id}/estado", new { vidaAtual = 1 })).StatusCode);
    }
}

public class EspacosDeMagiaNovosTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public EspacosDeMagiaNovosTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Personagem_novo_nasce_com_os_espacos_de_magia_disponiveis()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();

        // o formulário manda "atual = 0" (não pergunta quantos já foram gastos)
        var criado = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Lyra", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Maga", nivel = 5, raca = "Elfa", antecedente = "Soldado", subclasse = "Campeão" },
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 20, vidaAtual = 20, deslocamento = "9m" },
            personagemConjuracao = new { atributoConjuracao = "Inteligencia", slotsNivel1Max = 4, slotsNivel1Atual = 0, slotsNivel2Max = 3, slotsNivel2Atual = 1 },
        });
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var conjuracao = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("personagemConjuracao");
        Assert.Equal(4, conjuracao.GetProperty("slotsNivel1Atual").GetInt32()); // 0 = não informado → cheio
        Assert.Equal(1, conjuracao.GetProperty("slotsNivel2Atual").GetInt32()); // o que foi informado é respeitado
        Assert.Equal(0, conjuracao.GetProperty("slotsNivel3Atual").GetInt32()); // sem espaços desse círculo
    }
}
