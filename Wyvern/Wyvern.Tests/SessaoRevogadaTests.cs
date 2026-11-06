using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// O JWT sozinho não é revogável, então cada requisição confere no servidor se a sessão por trás dele (claim "sid", a
/// família do refresh token) ainda vale: logout, reuso detectado ou usuário desativado derrubam o access token na hora.
/// </summary>
public class SessaoRevogadaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public SessaoRevogadaTests(WyvernApiFactory factory) => _factory = factory;

    private record Sessao(int UsuarioId, string Token, string RefreshToken);

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private HttpClient ClienteNativo()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        return client;
    }

    private async Task<Sessao> CadastrarAsync()
    {
        var resposta = await _factory.CadastrarEEntrarAsync(ClienteNativo(), $"{Guid.NewGuid():N}@teste.com", "Sessao");
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<Sessao>(Web))!;
    }

    private Task<HttpResponseMessage> ComBearerAsync(HttpMethod metodo, string url, string token)
    {
        var pedido = new HttpRequestMessage(metodo, url);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _factory.NovoClienteWeb().SendAsync(pedido);
    }

    private async Task<HttpStatusCode> MeAsync(string token) =>
        (await ComBearerAsync(HttpMethod.Get, "/Auth/me", token)).StatusCode;

    private Task<HttpResponseMessage> TrocarAsync(string refreshToken) =>
        ClienteNativo().PostAsJsonAsync("/Auth/refresh", new { refreshToken });

    private static string Hash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    [Fact]
    public async Task Logout_derruba_o_access_token_na_hora()
    {
        var sessao = await CadastrarAsync();
        Assert.Equal(HttpStatusCode.OK, await MeAsync(sessao.Token)); // aqui a sessão entra no cache

        var logout = await ClienteNativo().PostAsJsonAsync("/Auth/logout", new { sessao.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(sessao.Token));
    }

    [Fact]
    public async Task Logout_de_uma_sessao_nao_derruba_outra_do_mesmo_usuario()
    {
        var email = $"{Guid.NewGuid():N}@teste.com";
        var cliente = ClienteNativo();
        var primeira = (await (await _factory.CadastrarEEntrarAsync(cliente, email, "Dois"))
            .Content.ReadFromJsonAsync<Sessao>(Web))!;
        var segunda = (await (await ClienteNativo().PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" }))
            .Content.ReadFromJsonAsync<Sessao>(Web))!; // outro aparelho: outra família
        await MeAsync(primeira.Token);
        await MeAsync(segunda.Token);

        await ClienteNativo().PostAsJsonAsync("/Auth/logout", new { primeira.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(primeira.Token));
        Assert.Equal(HttpStatusCode.OK, await MeAsync(segunda.Token));
    }

    [Fact]
    public async Task Reuso_de_refresh_depois_da_tolerancia_derruba_tambem_os_access_tokens_da_familia()
    {
        var a = await CadastrarAsync();
        var b = (await (await TrocarAsync(a.RefreshToken)).Content.ReadFromJsonAsync<Sessao>(Web))!;
        Assert.Equal(HttpStatusCode.OK, await MeAsync(b.Token));

        using (var escopo = _factory.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            var hash = Hash(a.RefreshToken);
            (await db.RefreshTokens.SingleAsync(t => t.TokenHash == hash)).UsadoEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await TrocarAsync(a.RefreshToken)).StatusCode); // reuso: revoga a família

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(b.Token));
    }

    [Fact]
    public async Task Usuario_desativado_perde_o_acesso()
    {
        var sessao = await CadastrarAsync();
        using (var escopo = _factory.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            (await db.Usuarios.SingleAsync(u => u.UsuarioId == sessao.UsuarioId)).Ativo = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(sessao.Token));
    }

    [Fact]
    public async Task Token_sem_sid_nao_e_aceito_porque_nao_teria_como_ser_revogado()
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-de-teste-com-mais-de-trinta-e-dois-caracteres"));
        var jwt = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "WyvernApi", audience: "WyvernFront",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim("role", "Jogador") },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)));

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(jwt));
    }

    [Fact]
    public async Task Ticket_de_hub_de_uma_sessao_revogada_nao_abre_o_hub()
    {
        var sessao = await CadastrarAsync();
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/Auth/hub-ticket");
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        var ticket = (await (await _factory.NovoClienteWeb().SendAsync(pedido)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("ticket").GetString()!;
        var negociar = () => _factory.NovoClienteWeb().PostAsync(
            $"/mesahub/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(ticket)}", null);
        Assert.Equal(HttpStatusCode.OK, (await negociar()).StatusCode);

        await ClienteNativo().PostAsJsonAsync("/Auth/logout", new { sessao.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, (await negociar()).StatusCode);
    }
}
