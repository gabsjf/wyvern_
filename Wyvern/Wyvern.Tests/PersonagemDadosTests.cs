using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class PersonagemDadosTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public PersonagemDadosTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Antecedente_subclasse_e_tamanho_sao_salvos_e_devolvidos()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();

        var criado = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Kaelith Vorn", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Bardo", raca = "Meio-elfo", nivel = 5, alinhamento = "Neutro", antecedente = "Artista", subclasse = "Colégio da Bravura", tamanho = "Médio" },
        });
        criado.EnsureSuccessStatusCode();
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var lido = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("personagemPlayer");

        Assert.Equal("Artista", lido.GetProperty("antecedente").GetString());
        Assert.Equal("Colégio da Bravura", lido.GetProperty("subclasse").GetString());
        Assert.Equal("Médio", lido.GetProperty("tamanho").GetString());
    }

    [Fact]
    public async Task Proficiencia_em_salvaguardas_e_salva_e_devolvida()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();

        var criado = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Kaelith", campanhaId, tipoId = 1,
            atributo = new
            {
                forca = 8, destreza = 14, constituicao = 12, inteligencia = 11, sabedoria = 10, carisma = 18,
                proficienciaSalvaguardaDestreza = true, proficienciaSalvaguardaCarisma = true,
            },
        });
        criado.EnsureSuccessStatusCode();
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var atributo = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("atributo");

        Assert.True(atributo.GetProperty("proficienciaSalvaguardaDestreza").GetBoolean());
        Assert.True(atributo.GetProperty("proficienciaSalvaguardaCarisma").GetBoolean());
        Assert.False(atributo.GetProperty("proficienciaSalvaguardaForca").GetBoolean());
    }

    [Fact]
    public async Task Editar_o_personagem_atualiza_antecedente_e_subclasse()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var criado = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Kaelith", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Bardo", raca = "Meio-elfo", nivel = 1, alinhamento = "Neutro", antecedente = "Artista" },
        });
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var edicao = await mestre.PutAsJsonAsync($"/Personagem/{id}", new
        {
            nome = "Kaelith", tipoId = 1,
            personagemPlayer = new { personagemId = id, classe = "Bardo", raca = "Meio-elfo", nivel = 3, alinhamento = "Neutro", antecedente = "Nobre", subclasse = "Colégio do Conhecimento" },
        });
        edicao.EnsureSuccessStatusCode();

        var lido = (await mestre.GetFromJsonAsync<JsonElement>($"/Personagem/{id}")).GetProperty("personagemPlayer");
        Assert.Equal("Nobre", lido.GetProperty("antecedente").GetString());
        Assert.Equal("Colégio do Conhecimento", lido.GetProperty("subclasse").GetString());
        Assert.Equal(3, lido.GetProperty("nivel").GetInt32());
    }
}
