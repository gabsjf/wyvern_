using System.Net;
using System.Net.Http.Json;
using Wyvern.Api.Extensions;

namespace Wyvern.Tests;

/// <summary>
/// Sessão em cookie: CORS com credenciais precisa de lista exata de origens (nunca "*"), e toda mutação feita só com o
/// cookie precisa passar pelo cabeçalho anti-CSRF e, se trouxer Origin, vir de uma origem permitida.
/// </summary>
public class CorsCsrfTests : IClassFixture<WyvernApiFactory>
{
    // appsettings.Development.json
    private const string OrigemPermitida = "http://localhost:4200";
    private const string OrigemEstranha = "https://site-malicioso.example";

    private readonly WyvernApiFactory _factory;

    public CorsCsrfTests(WyvernApiFactory factory) => _factory = factory;

    private static HttpRequestMessage Preflight(string origem, string metodo, string cabecalhos = "content-type,x-requested-with")
    {
        var pedido = new HttpRequestMessage(HttpMethod.Options, "/Campanha");
        pedido.Headers.Add("Origin", origem);
        pedido.Headers.Add("Access-Control-Request-Method", metodo);
        pedido.Headers.Add("Access-Control-Request-Headers", cabecalhos);
        return pedido;
    }

    private static string? Cabecalho(HttpResponseMessage resposta, string nome) =>
        resposta.Headers.TryGetValues(nome, out var valores) ? valores.Single() : null;

    // ---------- CORS ----------

    [Fact]
    public async Task Preflight_de_origem_permitida_devolve_a_propria_origem_com_credenciais_e_cache()
    {
        var resposta = await _factory.NovoClienteWeb().SendAsync(Preflight(OrigemPermitida, "POST"));

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(OrigemPermitida, Cabecalho(resposta, "Access-Control-Allow-Origin")); // nunca "*"
        Assert.Equal("true", Cabecalho(resposta, "Access-Control-Allow-Credentials"));
        Assert.Equal("600", Cabecalho(resposta, "Access-Control-Max-Age"));
    }

    [Fact]
    public async Task Preflight_de_origem_estranha_nao_recebe_permissao()
    {
        var resposta = await _factory.NovoClienteWeb().SendAsync(Preflight(OrigemEstranha, "POST"));

        Assert.Null(Cabecalho(resposta, "Access-Control-Allow-Origin"));
        Assert.Null(Cabecalho(resposta, "Access-Control-Allow-Credentials"));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Preflight_aceita_os_metodos_que_a_api_usa(string metodo)
    {
        var resposta = await _factory.NovoClienteWeb().SendAsync(Preflight(OrigemPermitida, metodo));

        Assert.Equal(OrigemPermitida, Cabecalho(resposta, "Access-Control-Allow-Origin"));
        Assert.Contains(metodo, Cabecalho(resposta, "Access-Control-Allow-Methods")!.Split(',', StringSplitOptions.TrimEntries));
    }

    [Theory]
    [InlineData("TRACE")]
    [InlineData("CONNECT")]
    public async Task Preflight_nao_autoriza_metodos_que_a_api_nao_usa(string metodo)
    {
        var resposta = await _factory.NovoClienteWeb().SendAsync(Preflight(OrigemPermitida, metodo));

        // O servidor responde ao preflight, mas sem listar o método em Allow-Methods; é isso que faz o navegador barrar.
        var permitidos = Cabecalho(resposta, "Access-Control-Allow-Methods")?.Split(',', StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
        Assert.DoesNotContain(metodo, permitidos);
    }

    [Fact]
    public async Task Preflight_aceita_os_cabecalhos_do_signalr_e_do_app_nativo()
    {
        var resposta = await _factory.NovoClienteWeb().SendAsync(Preflight(OrigemPermitida, "POST",
            "authorization,content-type,x-requested-with,x-signalr-user-agent,x-wyvern-client"));

        Assert.Equal(OrigemPermitida, Cabecalho(resposta, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Resposta_a_origem_permitida_repete_a_origem_e_a_estranha_nao_recebe_nada()
    {
        var web = _factory.NovoClienteWeb();
        var permitida = new HttpRequestMessage(HttpMethod.Get, "/health");
        permitida.Headers.Add("Origin", OrigemPermitida);
        var estranha = new HttpRequestMessage(HttpMethod.Get, "/health");
        estranha.Headers.Add("Origin", OrigemEstranha);

        Assert.Equal(OrigemPermitida, Cabecalho(await web.SendAsync(permitida), "Access-Control-Allow-Origin"));
        Assert.Null(Cabecalho(await web.SendAsync(estranha), "Access-Control-Allow-Origin"));
    }

    // ---------- lista de origens na configuração ----------

    [Fact]
    public void Origens_validas_sao_aceitas()
    {
        var origens = CorsConfiguracao.Validar(new[]
        {
            "https://wyvern.sbs", "https://www.wyvern.sbs", "https://localhost", "capacitor://localhost",
            "http://localhost:4200", "http://127.0.0.1:4200",
        });

        Assert.Equal(6, origens.Length);
        Assert.Empty(CorsConfiguracao.Validar(Array.Empty<string>())); // sem entradas: nenhuma origem externa
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.wyvern.sbs")]
    [InlineData("")]
    [InlineData("wyvern.sbs")]
    [InlineData("/api")]
    [InlineData("https://wyvern.sbs/")]
    [InlineData("https://wyvern.sbs/app")]
    [InlineData("https://wyvern.sbs?x=1")]
    [InlineData("http://wyvern.sbs")]
    public void Origens_invalidas_derrubam_a_inicializacao_com_mensagem_clara(string origem)
    {
        var erro = Assert.Throws<InvalidOperationException>(() => CorsConfiguracao.Validar(new[] { "https://wyvern.sbs", origem }));

        Assert.Contains("Cors:AllowedOrigins", erro.Message);
    }

    // ---------- CSRF nas mutações feitas só com o cookie ----------

    private async Task<string> CookieDeSessaoAsync()
    {
        var cadastro = await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), $"{Guid.NewGuid():N}@teste.com", "Csrf");
        cadastro.EnsureSuccessStatusCode();
        return cadastro.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("wyvern_at=", StringComparison.Ordinal)).Split(';')[0];
    }

    private async Task<HttpStatusCode> CriarCampanhaAsync(string cookie, string? origem, bool comCabecalho)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/Campanha") { Content = JsonContent.Create(new { nome = "Mesa", descricao = "x" }) };
        pedido.Headers.Add("Cookie", cookie);
        if (origem != null) pedido.Headers.Add("Origin", origem);
        if (comCabecalho) pedido.Headers.Add("X-Requested-With", "wyvern");
        return (await _factory.NovoClienteWeb().SendAsync(pedido)).StatusCode;
    }

    [Fact]
    public async Task Mutacao_com_cookie_exige_o_cabecalho_mesmo_de_origem_permitida()
    {
        var cookie = await CookieDeSessaoAsync();

        Assert.Equal(HttpStatusCode.Forbidden, await CriarCampanhaAsync(cookie, OrigemPermitida, comCabecalho: false));
    }

    [Fact]
    public async Task Mutacao_com_cookie_e_cabecalho_mas_de_origem_estranha_e_barrada()
    {
        var cookie = await CookieDeSessaoAsync();

        Assert.Equal(HttpStatusCode.Forbidden, await CriarCampanhaAsync(cookie, OrigemEstranha, comCabecalho: true));
        Assert.Equal(HttpStatusCode.Forbidden, await CriarCampanhaAsync(cookie, "null", comCabecalho: true)); // página sandboxed / data:
    }

    [Fact]
    public async Task Mutacao_com_cookie_cabecalho_e_origem_permitida_passa()
    {
        var cookie = await CookieDeSessaoAsync();

        Assert.NotEqual(HttpStatusCode.Forbidden, await CriarCampanhaAsync(cookie, OrigemPermitida, comCabecalho: true));
        Assert.NotEqual(HttpStatusCode.Forbidden, await CriarCampanhaAsync(cookie, null, comCabecalho: true)); // sem Origin: vale o cabeçalho
    }

    [Fact]
    public async Task Refresh_e_logout_tambem_nao_aceitam_origem_estranha()
    {
        var web = _factory.NovoClienteWeb();
        var cadastro = await _factory.CadastrarEEntrarAsync(web, $"{Guid.NewGuid():N}@teste.com", "Csrf");
        var cookies = string.Join("; ", cadastro.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

        foreach (var rota in new[] { "/Auth/refresh", "/Auth/logout" })
        {
            var pedido = new HttpRequestMessage(HttpMethod.Post, rota);
            pedido.Headers.Add("Cookie", cookies);
            pedido.Headers.Add("Origin", OrigemEstranha);
            pedido.Headers.Add("X-Requested-With", "wyvern");
            Assert.Equal(HttpStatusCode.Forbidden, (await web.SendAsync(pedido)).StatusCode);
        }
    }

    [Fact]
    public async Task Cliente_nativo_com_Authorization_nao_depende_de_Origin_nem_do_cabecalho()
    {
        var (cliente, _) = await _factory.NovoUsuarioAsync();

        var resposta = await cliente.PostAsJsonAsync("/Campanha", new { nome = "Nativa", descricao = "x" });

        Assert.NotEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ---------- cookie ----------

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)] // produção atrás de proxy que não repassou o HTTPS: continua Secure
    [InlineData(false, true, false)] // só o localhost de desenvolvimento, que é HTTP, fica sem
    public void Cookie_so_deixa_de_ser_Secure_no_desenvolvimento_em_http(bool https, bool desenvolvimento, bool esperado)
    {
        Assert.Equal(esperado, SessionCookie.ExigeSecure(https, desenvolvimento));
    }
}
