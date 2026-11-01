using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class ConviteTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public ConviteTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task<string> TokenAsync(HttpClient mestre, int campanhaId, bool renovar = false)
    {
        var resposta = await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite{(renovar ? "?renovar=true" : "")}", null);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Gerar_convite_de_novo_devolve_o_mesmo_link()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var primeiro = await TokenAsync(mestre, campanhaId);
        var segundo = await TokenAsync(mestre, campanhaId);

        Assert.Equal(primeiro, segundo);
    }

    [Fact]
    public async Task Renovar_invalida_o_link_anterior()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var antigo = await TokenAsync(mestre, campanhaId);
        var novo = await TokenAsync(mestre, campanhaId, renovar: true);

        Assert.NotEqual(antigo, novo);
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.PostAsync($"/Campanha/join/{antigo}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsync($"/Campanha/join/{novo}", null)).StatusCode);
    }

    [Fact]
    public async Task Revogar_desativa_o_link_e_so_o_mestre_pode()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var token = await TokenAsync(mestre, campanhaId);
        (await jogador.PostAsync($"/Campanha/join/{token}", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/invite")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/invite")).StatusCode);

        var (outro, _) = await _factory.NovoUsuarioAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await outro.PostAsync($"/Campanha/join/{token}", null)).StatusCode);
    }

    [Fact]
    public async Task Mestre_no_proprio_link_recebe_400_e_entrar_duas_vezes_e_idempotente()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var token = await TokenAsync(mestre, campanhaId);

        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.PostAsync($"/Campanha/join/{token}", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsync($"/Campanha/join/{token}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsync($"/Campanha/join/{token}", null)).StatusCode);

        var campanhas = await jogador.GetFromJsonAsync<JsonElement>("/Campanha");
        Assert.Equal(1, campanhas.GetArrayLength());
    }
}
