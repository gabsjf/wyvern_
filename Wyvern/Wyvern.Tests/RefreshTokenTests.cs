using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// Access token curto + refresh token opaco guardado como hash e trocado a cada uso. Reusar um refresh já trocado,
/// fora da tolerância, revoga a família inteira.
/// </summary>
public class RefreshTokenTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public RefreshTokenTests(WyvernApiFactory factory) => _factory = factory;

    private record Tokens(string Token, string RefreshToken);

    private HttpClient NovoClienteNativo()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        return client;
    }

    private async Task<Tokens> CadastrarNativoAsync()
    {
        var resposta = await _factory.CadastrarEEntrarAsync(NovoClienteNativo(), $"{Guid.NewGuid():N}@teste.com", "Refresh");
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<Tokens>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }

    private async Task<HttpResponseMessage> TrocarNativoAsync(string refreshToken) =>
        await NovoClienteNativo().PostAsJsonAsync("/Auth/refresh", new { refreshToken });

    private static async Task<Tokens> LerTokensAsync(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<Tokens>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;

    private static string Hash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Mexe direto no banco para simular a passagem do tempo (a tolerância é de segundos).</summary>
    private async Task AlterarAsync(string refreshToken, Action<Wyvern.Domain.Entities.RefreshToken> alterar)
    {
        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
        var hash = Hash(refreshToken);
        var token = await db.RefreshTokens.SingleAsync(t => t.TokenHash == hash);
        alterar(token);
        await db.SaveChangesAsync();
    }

    private static string ParCookie(HttpResponseMessage resposta, string nome) =>
        resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(nome + "=", StringComparison.Ordinal)).Split(';')[0];

    [Fact]
    public async Task Login_na_web_grava_refresh_em_cookie_HttpOnly_restrito_a_Auth_e_nao_o_devolve_no_corpo()
    {
        var resposta = await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), $"{Guid.NewGuid():N}@teste.com", "Web");

        resposta.EnsureSuccessStatusCode();
        var cookie = resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("wyvern_rt=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/Auth", cookie, StringComparison.OrdinalIgnoreCase);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.TryGetProperty("refreshToken", out _));
        Assert.False(corpo.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Access_token_dura_poucos_minutos()
    {
        var tokens = await CadastrarNativoAsync();

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(tokens.Token);

        Assert.True(jwt.ValidTo - DateTime.UtcNow <= TimeSpan.FromMinutes(16));
    }

    [Fact]
    public async Task Refresh_troca_o_par_de_tokens_e_o_novo_access_funciona()
    {
        var antigo = await CadastrarNativoAsync();

        var resposta = await TrocarNativoAsync(antigo.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var novo = await LerTokensAsync(resposta);
        Assert.NotEqual(antigo.RefreshToken, novo.RefreshToken);
        Assert.NotEqual(antigo.Token, novo.Token);

        var me = new HttpRequestMessage(HttpMethod.Get, "/Auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", novo.Token);
        Assert.Equal(HttpStatusCode.OK, (await _factory.NovoClienteWeb().SendAsync(me)).StatusCode);
    }

    [Fact]
    public async Task Banco_guarda_so_o_hash_do_refresh_token()
    {
        var tokens = await CadastrarNativoAsync();

        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
        var salvo = await db.RefreshTokens.SingleAsync(t => t.TokenHash == Hash(tokens.RefreshToken));

        Assert.NotEqual(tokens.RefreshToken, salvo.TokenHash);
        Assert.Equal(64, salvo.TokenHash.Length);
    }

    [Fact]
    public async Task Reuso_logo_depois_da_troca_so_nega_e_nao_derruba_a_sessao()
    {
        var a = await CadastrarNativoAsync();
        var b = await LerTokensAsync(await TrocarNativoAsync(a.RefreshToken));

        // duas abas / rede ruim: o token antigo chega de novo segundos depois
        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync(a.RefreshToken)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await TrocarNativoAsync(b.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Reuso_depois_da_tolerancia_revoga_a_familia_inteira()
    {
        var a = await CadastrarNativoAsync();
        var b = await LerTokensAsync(await TrocarNativoAsync(a.RefreshToken));
        await AlterarAsync(a.RefreshToken, t => t.UsadoEm = DateTime.UtcNow.AddMinutes(-1));

        // quem roubou A tenta usá-lo: nega e derruba também o B, que o dono legítimo tem
        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync(a.RefreshToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync(b.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Familias_de_logins_diferentes_sao_independentes()
    {
        var primeiro = await CadastrarNativoAsync();
        var outro = await CadastrarNativoAsync();
        await TrocarNativoAsync(primeiro.RefreshToken);
        await AlterarAsync(primeiro.RefreshToken, t => t.UsadoEm = DateTime.UtcNow.AddMinutes(-1));
        await TrocarNativoAsync(primeiro.RefreshToken); // reuso: revoga só a família do primeiro

        Assert.Equal(HttpStatusCode.OK, (await TrocarNativoAsync(outro.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_vencido_desconhecido_ou_ausente_e_401()
    {
        var tokens = await CadastrarNativoAsync();
        await AlterarAsync(tokens.RefreshToken, t => t.ExpiraEm = DateTime.UtcNow.AddMinutes(-1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync(tokens.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync("token-que-nao-existe")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await NovoClienteNativo().PostAsync("/Auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_revoga_o_refresh_token()
    {
        var tokens = await CadastrarNativoAsync();

        var logout = await NovoClienteNativo().PostAsJsonAsync("/Auth/logout", new { tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarNativoAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Na_web_o_refresh_usa_so_o_cookie_e_exige_o_cabecalho_anti_CSRF()
    {
        var web = _factory.NovoClienteWeb();
        var cadastro = await _factory.CadastrarEEntrarAsync(web, $"{Guid.NewGuid():N}@teste.com", "Web");
        var cookieRefresh = ParCookie(cadastro, "wyvern_rt");

        // o access cookie já venceu (só o refresh sobrou) e a requisição não traz o cabeçalho anti-CSRF
        var semCabecalho = new HttpRequestMessage(HttpMethod.Post, "/Auth/refresh");
        semCabecalho.Headers.Add("Cookie", cookieRefresh);
        Assert.Equal(HttpStatusCode.Forbidden, (await web.SendAsync(semCabecalho)).StatusCode);

        var comCabecalho = new HttpRequestMessage(HttpMethod.Post, "/Auth/refresh");
        comCabecalho.Headers.Add("Cookie", cookieRefresh);
        comCabecalho.Headers.Add("X-Requested-With", "wyvern");
        var resposta = await web.SendAsync(comCabecalho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotEqual(cookieRefresh, ParCookie(resposta, "wyvern_rt"));
        Assert.StartsWith("wyvern_at=", ParCookie(resposta, "wyvern_at"));
    }
}
