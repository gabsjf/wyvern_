using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Wyvern.Tests;

/// <summary>API com relógio controlável, para testar a validade dos convites.</summary>
public class FabricaComRelogio : WyvernApiFactory
{
    public FakeTimeProvider Relogio { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(Relogio));
    }
}

public class ConviteExpiracaoTests : IClassFixture<FabricaComRelogio>
{
    private readonly FabricaComRelogio _fabrica;

    public ConviteExpiracaoTests(FabricaComRelogio fabrica) => _fabrica = fabrica;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task<JsonElement> ConviteAsync(HttpClient mestre, int campanhaId, bool renovar = false)
    {
        var r = await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite{(renovar ? "?renovar=true" : "")}", null);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Convite_vale_14_dias_e_depois_expira()
    {
        var (mestre, _) = await _fabrica.NovoUsuarioAsync();
        var (jogador, _) = await _fabrica.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var convite = await ConviteAsync(mestre, campanhaId);
        var token = convite.GetProperty("token").GetString();

        var expiraEm = convite.GetProperty("expiraEm").GetDateTime();
        Assert.InRange((expiraEm - _fabrica.Relogio.GetUtcNow().UtcDateTime).TotalDays, 13.9, 14.1);

        // No 13º dia ainda vale; no 15º não.
        _fabrica.Relogio.Advance(TimeSpan.FromDays(13));
        var (a, _) = await _fabrica.NovoUsuarioAsync();
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsync($"/Campanha/join/{token}", null)).StatusCode);

        _fabrica.Relogio.Advance(TimeSpan.FromDays(2));
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.PostAsync($"/Campanha/join/{token}", null)).StatusCode);
    }

    [Fact]
    public async Task Pedir_convite_de_novo_depois_de_expirar_gera_link_novo()
    {
        var (mestre, _) = await _fabrica.NovoUsuarioAsync();
        var (jogador, _) = await _fabrica.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var antigo = (await ConviteAsync(mestre, campanhaId)).GetProperty("token").GetString();

        _fabrica.Relogio.Advance(TimeSpan.FromDays(20));
        var novo = (await ConviteAsync(mestre, campanhaId)).GetProperty("token").GetString();

        Assert.NotEqual(antigo, novo);
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.PostAsync($"/Campanha/join/{antigo}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsync($"/Campanha/join/{novo}", null)).StatusCode);
    }
}
