using System.Net.Http.Json;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>Os e-mails da conta saem em HTML e em texto puro, com o mesmo link e sem deixar o nome do usuário virar HTML.</summary>
public class ModeloDeEmailTests : IClassFixture<WyvernApiFactory>
{
    private const string Url = "https://wyvern.sbs/redefinir-senha?token=12.abc-_&x=1";

    private readonly WyvernApiFactory _factory;

    public ModeloDeEmailTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public void Redefinicao_tem_botao_com_o_link_e_versao_em_texto_com_o_mesmo_link()
    {
        var corpo = ModeloDeEmail.Redefinicao("Ana", Url);

        Assert.Contains("href=\"https://wyvern.sbs/redefinir-senha?token=12.abc-_&amp;x=1\"", corpo.Html); // & escapado no atributo
        Assert.Contains("Redefinir senha", corpo.Html);
        Assert.Contains("1 hora", corpo.Html);
        Assert.Contains(Url, corpo.Texto);
        Assert.StartsWith("Olá, Ana.", corpo.Texto);
    }

    [Fact]
    public void Nome_com_html_e_escapado()
    {
        var corpo = ModeloDeEmail.Confirmacao("<script>alert(1)</script>", Url);

        Assert.DoesNotContain("<script>", corpo.Html);
        Assert.Contains("&lt;script&gt;", corpo.Html);
    }

    [Fact]
    public void Os_tres_modelos_trazem_html_completo()
    {
        foreach (var corpo in new[] { ModeloDeEmail.Confirmacao("Ana", Url), ModeloDeEmail.Redefinicao("Ana", Url), ModeloDeEmail.ContaJaExiste("Ana", Url) })
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
