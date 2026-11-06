using System.Net.Http.Json;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>Os e-mails da conta saem em HTML e em texto puro, com o mesmo link e sem deixar o nome do usuário virar HTML.</summary>
public class ModeloDeEmailTests : IClassFixture<WyvernApiFactory>
{
    private const string Url = "https://wyvern.sbs/redefinir-senha?token=12.abc-_&x=1";
    private const string Logo = "https://wyvern.sbs/assets/images/logo-dragao-email.png";

    private readonly WyvernApiFactory _factory;

    public ModeloDeEmailTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public void Redefinicao_tem_botao_com_o_link_e_versao_em_texto_com_o_mesmo_link()
    {
        var corpo = ModeloDeEmail.Redefinicao("Ana", Url, Logo);

        Assert.Contains("href=\"https://wyvern.sbs/redefinir-senha?token=12.abc-_&amp;x=1\"", corpo.Html); // & escapado no atributo
        Assert.Contains("Criar nova senha", corpo.Html);
        Assert.Contains("1 hora", corpo.Html);
        Assert.Contains(Url, corpo.Texto);
        Assert.Contains("Olá, Ana.", corpo.Texto);
    }

    [Fact]
    public void Todos_os_modelos_trazem_a_logo_com_texto_alternativo()
    {
        foreach (var corpo in new[] { ModeloDeEmail.Confirmacao("Ana", Url, Logo), ModeloDeEmail.Redefinicao("Ana", Url, Logo), ModeloDeEmail.ContaJaExiste("Ana", Url, Logo) })
            Assert.Contains($"<img src=\"{Logo}\"", corpo.Html);
        Assert.Contains("alt=\"Wyvern\"", ModeloDeEmail.Redefinicao("Ana", Url, Logo).Html);
    }

    [Fact]
    public void Nome_com_html_e_escapado()
    {
        var corpo = ModeloDeEmail.Confirmacao("<script>alert(1)</script>", Url, Logo);

        Assert.DoesNotContain("<script>", corpo.Html);
        Assert.Contains("&lt;script&gt;", corpo.Html);
    }

    [Fact]
    public void Os_tres_modelos_trazem_html_completo()
    {
        foreach (var corpo in new[] { ModeloDeEmail.Confirmacao("Ana", Url, Logo), ModeloDeEmail.Redefinicao("Ana", Url, Logo), ModeloDeEmail.ContaJaExiste("Ana", Url, Logo) })
        {
            Assert.StartsWith("<!doctype html>", corpo.Html);
            Assert.EndsWith("</html>", corpo.Html);
            Assert.Contains("WYVERN", corpo.Html);
        }
    }

    [Fact]
    public async Task Email_de_redefinicao_enviado_pela_api_leva_html_e_texto()
    {
        var client = _factory.NovoClienteWeb();
        var email = $"{Guid.NewGuid():N}@teste.com";
        await _factory.CadastrarEEntrarAsync(client, email);

        await client.PostAsJsonAsync("/Auth/esqueci-senha", new { email });

        var enviado = _factory.Emails.Para(email).Last(e => e.Assunto.StartsWith("Redefinição"));
        Assert.NotNull(enviado.Html);
        Assert.Contains("/redefinir-senha?token=", enviado.Html);
        Assert.Contains("/redefinir-senha?token=", enviado.Corpo);
    }
}
