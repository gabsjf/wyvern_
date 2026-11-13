using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class PdfTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public PdfTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Exportar_pdf_encontra_o_modelo_e_devolve_um_pdf()
    {
        // Guarda contra o modelo (Templates/ficha55.pdf) não ser copiado junto com a aplicação,
        // o que quebrava a exportação na imagem Docker.
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var criado = await mestre.PostAsJsonAsync("/Personagem", Fichas.Heroi("Kaelith Vorn", campanhaId));
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var resposta = await mestre.GetAsync($"/Personagem/{personagemId}/export-pdf");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/pdf", resposta.Content.Headers.ContentType?.MediaType);
        var bytes = await resposta.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Exportar_e_importar_pela_api_cria_o_personagem_com_os_dados()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var criado = await mestre.PostAsJsonAsync("/Personagem", new
        {
            nome = "Kaelith Vorn", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Bardo", raca = "Meio-elfo", nivel = 5, alinhamento = "Neutro", antecedente = "Artista", subclasse = "Colégio da Bravura" },
            atributo = new { forca = 8, destreza = 14, constituicao = 12, inteligencia = 11, sabedoria = 10, carisma = 18, proficienciaSalvaguardaCarisma = true },
            personagemCombate = Fichas.Combate,
            personagemMagias = new[] { new { nome = "Zombaria Viciosa", nivel = 0, escola = "Encantamento", descricao = "x" } },
        });
        Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
        var pdf = await (await mestre.GetAsync($"/Personagem/{id}/export-pdf")).Content.ReadAsByteArrayAsync();

        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(pdf), "file", "ficha.pdf" },
            { new StringContent(campanhaId.ToString()), "campanhaId" },
        };
        var resposta = await mestre.PostAsync("/Personagem/import-pdf", form);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var importado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(id, importado.GetProperty("personagemId").GetInt32());
        Assert.Equal("Kaelith Vorn", importado.GetProperty("nome").GetString());
        Assert.Equal("Bardo", importado.GetProperty("personagemPlayer").GetProperty("classe").GetString());
        Assert.Equal(5, importado.GetProperty("personagemPlayer").GetProperty("nivel").GetInt32());
        Assert.Equal(18, importado.GetProperty("atributo").GetProperty("carisma").GetInt32());
        Assert.True(importado.GetProperty("atributo").GetProperty("proficienciaSalvaguardaCarisma").GetBoolean());
    }

    [Fact]
    public async Task Exportar_pdf_de_personagem_de_outra_campanha_e_negado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var criado = await mestre.PostAsJsonAsync("/Personagem", Fichas.Heroi("Secreto", campanhaId));
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var resposta = await estranho.GetAsync($"/Personagem/{personagemId}/export-pdf");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
