using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// Esqueci a senha: a resposta não revela se há conta; o link do e-mail vale 1 hora, uma vez, e a troca derruba as sessões.
/// </summary>
public class RedefinicaoDeSenhaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public RedefinicaoDeSenhaTests(WyvernApiFactory factory) => _factory = factory;

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private static Task<HttpResponseMessage> Esqueci(HttpClient client, string email) =>
        client.PostAsJsonAsync("/Auth/esqueci-senha", new { email });

    private static Task<HttpResponseMessage> Redefinir(HttpClient client, string token, string novaSenha = "nova-senha-1") =>
        client.PostAsJsonAsync("/Auth/redefinir-senha", new { token, novaSenha });

    private static Task<HttpResponseMessage> Entrar(HttpClient client, string email, string senha) =>
        client.PostAsJsonAsync("/Auth/login", new { email, senha });

    [Fact]
    public async Task Resposta_e_identica_com_conta_existente_e_inexistente()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);

        var existente = await Esqueci(client, email);
        var inexistente = await Esqueci(client, NovoEmail());

        Assert.Equal(HttpStatusCode.Accepted, existente.StatusCode);
        Assert.Equal(existente.StatusCode, inexistente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Email_sem_conta_nao_recebe_nada()
    {
        var email = NovoEmail();

        await Esqueci(_factory.NovoClienteWeb(), email);

        Assert.Empty(_factory.Emails.Para(email));
    }

    [Fact]
    public async Task Conta_existente_recebe_o_link_e_a_nova_senha_substitui_a_antiga()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email, senha: "senha-antiga");

        await Esqueci(client, email);
        var token = _factory.TokenDeRedefinicao(email);
        Assert.Equal(HttpStatusCode.NoContent, (await Redefinir(client, token, "senha-nova")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "senha-nova")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(client, email, "senha-antiga")).StatusCode);
    }

    [Fact]
    public async Task Link_so_funciona_uma_vez()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        await Esqueci(client, email);
        var token = _factory.TokenDeRedefinicao(email);

        Assert.Equal(HttpStatusCode.NoContent, (await Redefinir(client, token, "primeira-troca")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(client, token, "segunda-troca")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "primeira-troca")).StatusCode);
    }

    [Fact]
    public async Task Pedir_outro_link_invalida_o_anterior()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        await Esqueci(client, email);
        var primeiro = _factory.TokenDeRedefinicao(email);
        await Esqueci(client, email);
        var segundo = _factory.TokenDeRedefinicao(email);

        Assert.NotEqual(primeiro, segundo);
        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(client, primeiro)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Redefinir(client, segundo)).StatusCode);
    }

    [Fact]
    public async Task Link_vencido_nao_redefine()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email, senha: "senha-antiga");
        await Esqueci(client, email);
        var token = _factory.TokenDeRedefinicao(email);
        using (var escopo = _factory.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            db.Usuarios.Single(u => u.Email == email).TokenRedefinicaoExpiraEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(client, token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "senha-antiga")).StatusCode);
    }

    [Fact]
    public async Task O_link_dura_uma_hora()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);

        await Esqueci(client, email);

        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
        var expira = db.Usuarios.Single(u => u.Email == email).TokenRedefinicaoExpiraEm!.Value;
        Assert.InRange(expira, DateTime.UtcNow.AddMinutes(55), DateTime.UtcNow.AddMinutes(61));
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("999999.abc")]
    public async Task Token_invalido_e_recusado(string token)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(_factory.NovoClienteWeb(), token)).StatusCode);
    }

    [Fact]
    public async Task Token_de_confirmacao_de_email_nao_serve_para_redefinir_senha()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await client.PostAsJsonAsync("/Auth/register", new { nome = "X", email, senha = "senha123" });
        var tokenDeConfirmacao = _factory.TokenDeConfirmacao(email);

        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(client, tokenDeConfirmacao)).StatusCode);
    }

    [Fact]
    public async Task Senha_nova_curta_e_recusada_e_nao_gasta_o_link()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        await Esqueci(client, email);
        var token = _factory.TokenDeRedefinicao(email);

        Assert.Equal(HttpStatusCode.BadRequest, (await Redefinir(client, token, "123")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Redefinir(client, token, "senha-valida")).StatusCode);
    }

    [Fact]
    public async Task Conta_nao_confirmada_recebe_o_link_de_confirmacao_e_nao_o_de_redefinicao()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await client.PostAsJsonAsync("/Auth/register", new { nome = "X", email, senha = "senha123" });

        await Esqueci(client, email);

        Assert.All(_factory.Emails.Para(email), e => Assert.StartsWith("Confirme", e.Assunto));
    }

    [Fact]
    public async Task Redefinir_a_senha_derruba_as_sessoes_abertas()
    {
        var email = NovoEmail();
        var nativo = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        nativo.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var login = await _factory.CadastrarEEntrarAsync(nativo, email);
        var sessao = (await login.Content.ReadFromJsonAsync<JsonElement>());
        var access = sessao.GetProperty("token").GetString()!;
        var refresh = sessao.GetProperty("refreshToken").GetString()!;

        var antes = new HttpRequestMessage(HttpMethod.Get, "/Auth/me");
        antes.Headers.Authorization = new("Bearer", access);
        Assert.Equal(HttpStatusCode.OK, (await nativo.SendAsync(antes)).StatusCode);

        await Esqueci(nativo, email);
        Assert.Equal(HttpStatusCode.NoContent, (await Redefinir(nativo, _factory.TokenDeRedefinicao(email))).StatusCode);

        var depois = new HttpRequestMessage(HttpMethod.Get, "/Auth/me");
        depois.Headers.Authorization = new("Bearer", access);
        Assert.Equal(HttpStatusCode.Unauthorized, (await nativo.SendAsync(depois)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await nativo.PostAsJsonAsync("/Auth/refresh", new { refreshToken = refresh })).StatusCode);
    }
}
