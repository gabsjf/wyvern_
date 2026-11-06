using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wyvern.Tests;

/// <summary>
/// A API só devolve JSON/PDF: CSP que proíbe tudo, nosniff, sem referrer e HSTS em produção. Os cabeçalhos
/// precisam estar também nas respostas de erro e de bloqueio.
/// </summary>
public class CabecalhosDeSegurancaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public CabecalhosDeSegurancaTests(WyvernApiFactory factory) => _factory = factory;

    private static string? Cabecalho(HttpResponseMessage resposta, string nome) =>
        resposta.Headers.TryGetValues(nome, out var valores) ? valores.Single() : null;

    private static void AssertCabecalhosBasicos(HttpResponseMessage resposta)
    {
        Assert.Equal("default-src 'none'; frame-ancestors 'none'; base-uri 'none'", Cabecalho(resposta, "Content-Security-Policy"));
        Assert.Equal("nosniff", Cabecalho(resposta, "X-Content-Type-Options"));
        Assert.Equal("no-referrer", Cabecalho(resposta, "Referrer-Policy"));
    }

    [Fact]
    public async Task Resposta_normal_traz_csp_nosniff_e_referrer_policy()
    {
        var resposta = await _factory.NovoClienteWeb().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        AssertCabecalhosBasicos(resposta);
    }

    [Fact]
    public async Task Resposta_de_erro_401_traz_os_cabecalhos()
    {
        var resposta = await _factory.NovoClienteWeb().GetAsync("/Campanha");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        AssertCabecalhosBasicos(resposta);
    }

    [Fact]
    public async Task Bloqueio_do_csrf_403_traz_os_cabecalhos()
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/combathub/negotiate");
        pedido.Headers.Add("Origin", "https://site-malicioso.example");

        var resposta = await _factory.NovoClienteWeb().SendAsync(pedido);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        AssertCabecalhosBasicos(resposta);
    }

    [Fact]
    public async Task Em_desenvolvimento_o_scalar_nao_recebe_csp_restritiva()
    {
        var resposta = await _factory.NovoClienteWeb().GetAsync("/scalar/v1");

        Assert.False(resposta.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("nosniff", Cabecalho(resposta, "X-Content-Type-Options"));
    }

    [Fact]
    public async Task Hsts_nao_e_enviado_em_desenvolvimento()
    {
        var resposta = await _factory.NovoClienteWeb().GetAsync("/health");

        Assert.False(resposta.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_e_enviado_em_producao_por_https()
    {
        await using var producao = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            // Em produção o envio de e-mail é obrigatório; o envio em si é o falso da fábrica.
            foreach (var chave in new[] { "Host", "Usuario", "Senha", "Remetente", "UrlFront" })
                b.UseSetting($"Email:{chave}", "x");
        });
        var cliente = producao.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.exemplo.test"), // HSTS ignora localhost
            HandleCookies = false
        });

        var resposta = await cliente.GetAsync("/health");

        Assert.Equal("max-age=31536000; includeSubDomains", Cabecalho(resposta, "Strict-Transport-Security"));
    }
}
