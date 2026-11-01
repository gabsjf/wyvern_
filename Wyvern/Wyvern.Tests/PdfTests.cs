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
        var criado = await mestre.PostAsJsonAsync("/Personagem", new { nome = "Kaelith Vorn", campanhaId, tipoId = 1 });
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var resposta = await mestre.GetAsync($"/Personagem/{personagemId}/export-pdf");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/pdf", resposta.Content.Headers.ContentType?.MediaType);
        var bytes = await resposta.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Exportar_pdf_de_personagem_de_outra_campanha_e_negado()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanha = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });
        var campanhaId = (await campanha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var criado = await mestre.PostAsJsonAsync("/Personagem", new { nome = "Secreto", campanhaId, tipoId = 1 });
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        var resposta = await estranho.GetAsync($"/Personagem/{personagemId}/export-pdf");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
