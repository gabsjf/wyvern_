using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// Quem não achou o link de confirmação (ou deixou vencer) pede outro. A resposta é sempre a mesma, para ninguém
/// descobrir quem tem conta, e o link só sai para conta que ainda espera confirmação, no máximo um por minuto.
/// </summary>
public class ReenvioDeConfirmacaoTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public ReenvioDeConfirmacaoTests(WyvernApiFactory factory) => _factory = factory;

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private static Task<HttpResponseMessage> Cadastrar(HttpClient client, string email) =>
        client.PostAsJsonAsync("/Auth/register", new { nome = "Reenvio", email, senha = "senha123" });

    private static Task<HttpResponseMessage> Reenviar(HttpClient client, string email) =>
        client.PostAsJsonAsync("/Auth/reenviar-confirmacao", new { email });

    private int Confirmacoes(string email) => _factory.Emails.Para(email).Count(e => e.Assunto.StartsWith("Confirme"));

    /// <summary>Faz de conta que o último link saiu há mais tempo, para passar do intervalo entre reenvios.</summary>
    private async Task EnvelhecerUltimoLinkAsync(string email, TimeSpan quanto)
    {
        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
        var user = db.Usuarios.Single(u => u.Email == email);
        user.TokenConfirmacaoExpiraEm -= quanto;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Resposta_e_identica_para_conta_pendente_confirmada_e_inexistente()
    {
        var client = _factory.NovoClienteWeb();
        var pendente = NovoEmail();
        await Cadastrar(client, pendente);
        await EnvelhecerUltimoLinkAsync(pendente, TimeSpan.FromMinutes(2));
        var confirmada = NovoEmail();
        await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), confirmada);

        var respostas = new[]
        {
            await Reenviar(client, pendente),
            await Reenviar(client, confirmada),
            await Reenviar(client, NovoEmail()),
        };

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Accepted, r.StatusCode));
        var corpos = await Task.WhenAll(respostas.Select(r => r.Content.ReadAsStringAsync()));
        Assert.Single(corpos.Distinct());
    }

    [Fact]
    public async Task Conta_pendente_recebe_link_novo_que_confirma_e_o_antigo_deixa_de_valer()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        var antigo = _factory.TokenDeConfirmacao(email);
        await EnvelhecerUltimoLinkAsync(email, TimeSpan.FromMinutes(2));

        await Reenviar(client, email);

        Assert.Equal(2, Confirmacoes(email));
        var novo = _factory.TokenDeConfirmacao(email);
        Assert.NotEqual(antigo, novo);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = antigo })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = novo })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" })).StatusCode);
    }

    [Fact]
    public async Task Link_vencido_pode_ser_substituido_por_um_novo()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        await EnvelhecerUltimoLinkAsync(email, TimeSpan.FromHours(25));

        await Reenviar(client, email);

        var token = _factory.TokenDeConfirmacao(email);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token })).StatusCode);
    }

    [Fact]
    public async Task Pedido_repetido_dentro_de_um_minuto_nao_manda_outro_email()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);
        var token = _factory.TokenDeConfirmacao(email);

        var resposta = await Reenviar(client, email);

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.Equal(1, Confirmacoes(email));
        // o link do cadastro continua valendo
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/confirmar-email", new { token })).StatusCode);
    }

    [Fact]
    public async Task Conta_ja_confirmada_nao_recebe_nada()
    {
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), email);
        var antes = _factory.Emails.Para(email).Count();

        await Reenviar(_factory.NovoClienteWeb(), email);

        Assert.Equal(antes, _factory.Emails.Para(email).Count());
    }

    [Fact]
    public async Task Login_sem_confirmar_traz_o_codigo_so_para_quem_acerta_a_senha()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await Cadastrar(client, email);

        var certa = await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" });
        var errada = await client.PostAsJsonAsync("/Auth/login", new { email, senha = "errada123" });

        Assert.Equal(HttpStatusCode.Unauthorized, certa.StatusCode);
        using (var corpo = JsonDocument.Parse(await certa.Content.ReadAsStringAsync()))
            Assert.Equal("email-nao-confirmado", corpo.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, errada.StatusCode);
        Assert.DoesNotContain("codigo", await errada.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Email_invalido_e_recusado()
    {
        var resposta = await Reenviar(_factory.NovoClienteWeb(), "nao-e-email");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
