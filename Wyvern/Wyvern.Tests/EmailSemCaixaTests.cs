using System.Net;
using System.Net.Http.Json;
using Wyvern.Domain.Regras;

namespace Wyvern.Tests;

/// <summary>
/// "Ana@X.com" e "ana@x.com" são a mesma conta: o e-mail é guardado e comparado sem espaços e em minúsculas.
/// </summary>
public class EmailSemCaixaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public EmailSemCaixaTests(WyvernApiFactory factory) => _factory = factory;

    private static string NovoEmail() => $"Ana.{Guid.NewGuid():N}@Teste.COM";

    [Theory]
    [InlineData("  Ana@X.com ", "ana@x.com")]
    [InlineData("ANA@X.COM", "ana@x.com")]
    [InlineData(null, "")]
    public void Normaliza_espacos_e_caixa(string? entrada, string esperado) =>
        Assert.Equal(esperado, EmailDaConta.Normalizar(entrada));

    [Fact]
    public async Task Cadastro_guarda_o_email_em_minusculas_e_o_login_aceita_qualquer_caixa()
    {
        var email = NovoEmail();
        var client = _factory.NovoClienteWeb();
        (await _factory.CadastrarEEntrarAsync(client, email.ToLowerInvariant())).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/Auth/login", new { email = "  " + email.ToUpperInvariant(), senha = "senha123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Cadastro_com_o_mesmo_email_em_outra_caixa_nao_cria_outra_conta()
    {
        var email = NovoEmail();
        var client = _factory.NovoClienteWeb();
        (await _factory.CadastrarEEntrarAsync(client, email.ToLowerInvariant())).EnsureSuccessStatusCode();
        var antes = _factory.Emails.Para(email.ToLowerInvariant()).Count();

        await client.PostAsJsonAsync("/Auth/register", new { nome = "Intrusa", email = email.ToUpperInvariant(), senha = "outra-senha" });

        // o dono recebe o aviso de "alguém tentou criar uma conta", e a senha nova não entra
        var aviso = _factory.Emails.Para(email.ToLowerInvariant()).Skip(antes).Single();
        Assert.StartsWith("Alguém tentou", aviso.Assunto);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "outra-senha" })).StatusCode);
    }

    [Fact]
    public async Task Troca_para_o_email_de_outra_conta_em_outra_caixa_nao_manda_link()
    {
        var ocupado = NovoEmail();
        await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), ocupado.ToLowerInvariant());
        var (cliente, _) = await _factory.NovoUsuarioAsync();

        var pedido = await cliente.PostAsJsonAsync("/Usuario/me/email", new { novoEmail = ocupado.ToUpperInvariant(), senhaAtual = "senha123" });

        Assert.Equal(HttpStatusCode.Accepted, pedido.StatusCode);
        Assert.DoesNotContain(_factory.Emails.Enviados, e => e.Assunto.StartsWith("Confirme o novo") && string.Equals(e.Para, ocupado, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Esqueci_a_senha_acha_a_conta_com_o_email_em_outra_caixa()
    {
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), email.ToLowerInvariant());

        await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/esqueci-senha", new { email = email.ToUpperInvariant() });

        Assert.Contains(_factory.Emails.Para(email.ToLowerInvariant()), e => e.Assunto.StartsWith("Redefinição"));
    }

    [Fact]
    public async Task Cadastro_com_maiusculas_grava_e_escreve_para_o_email_em_minusculas()
    {
        var email = NovoEmail();

        await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/register", new { nome = "Ana", email, senha = "senha123" });

        // o e-mail sai para o endereço gravado na conta
        Assert.Contains(_factory.Emails.Para(email.ToLowerInvariant()), e => e.Assunto.StartsWith("Confirme"));
        Assert.Empty(_factory.Emails.Para(email));
    }
}
