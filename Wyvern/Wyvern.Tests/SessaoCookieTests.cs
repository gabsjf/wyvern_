using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Wyvern.Tests;

/// <summary>A sessão da web é um cookie HttpOnly; o app nativo segue com Bearer.</summary>
public class SessaoCookieTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public SessaoCookieTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> CadastrarAsync(HttpClient client) =>
        await client.PostAsJsonAsync("/Auth/register", new { nome = "Web", email = $"{Guid.NewGuid():N}@teste.com", senha = "senha123" });

    private static string SetCookie(HttpResponseMessage resposta) =>
        resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("wyvern_at=", StringComparison.Ordinal));

    /// <summary>"wyvern_at=valor": o que o navegador reenviaria no cabeçalho Cookie.</summary>
    private static string ParCookie(HttpResponseMessage resposta) => SetCookie(resposta).Split(';')[0];

    private static HttpRequestMessage ComCookie(HttpMethod metodo, string url, string cookie)
    {
        var pedido = new HttpRequestMessage(metodo, url);
        pedido.Headers.Add("Cookie", cookie);
        return pedido;
    }

    [Fact]
    public async Task Cadastro_na_web_grava_cookie_HttpOnly_e_nao_devolve_o_token_no_corpo()
    {
        var resposta = await CadastrarAsync(_factory.NovoClienteWeb());

        resposta.EnsureSuccessStatusCode();
        var cookie = SetCookie(resposta);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.False((await resposta.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Cliente_nativo_recebe_o_token_no_corpo()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync();

        Assert.False(string.IsNullOrEmpty(sessao.Token));
    }

    [Fact]
    public async Task Me_com_cookie_devolve_o_usuario_e_sem_sessao_e_401()
    {
        var web = _factory.NovoClienteWeb();
        var cadastro = await CadastrarAsync(web);

        var logado = await web.SendAsync(ComCookie(HttpMethod.Get, "/Auth/me", ParCookie(cadastro)));
        Assert.Equal(HttpStatusCode.OK, logado.StatusCode);
        var corpo = await logado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Web", corpo.GetProperty("nome").GetString());
        Assert.False(corpo.TryGetProperty("token", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await web.GetAsync("/Auth/me")).StatusCode);
    }

    [Fact]
    public async Task Me_nao_entra_no_limite_de_tentativas_de_login()
    {
        using var fabrica = new WyvernApiFactory { AuthPermitLimit = 2 };
        var web = fabrica.NovoClienteWeb();
        var cadastro = await CadastrarAsync(web);
        var cookie = ParCookie(cadastro);

        for (var i = 0; i < 6; i++)
            Assert.Equal(HttpStatusCode.OK, (await web.SendAsync(ComCookie(HttpMethod.Get, "/Auth/me", cookie))).StatusCode);
    }

    [Fact]
    public async Task Logout_apaga_o_cookie()
    {
        var web = _factory.NovoClienteWeb();
        var cadastro = await CadastrarAsync(web);

        var pedido = ComCookie(HttpMethod.Post, "/Auth/logout", ParCookie(cadastro));
        pedido.Headers.Add("X-Requested-With", "wyvern");
        var resposta = await web.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var apagado = SetCookie(resposta);
        Assert.StartsWith("wyvern_at=;", apagado, StringComparison.Ordinal);
        Assert.Contains("expires=Thu, 01 Jan 1970", apagado, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Escrita_so_com_cookie_exige_o_cabecalho_anti_csrf()
    {
        var web = _factory.NovoClienteWeb();
        var cookie = ParCookie(await CadastrarAsync(web));
        var corpo = JsonContent.Create(new { nome = "Mesa", sistema = "D&D 5.5e" });

        var semCabecalho = ComCookie(HttpMethod.Post, "/Campanha", cookie);
        semCabecalho.Content = corpo;
        Assert.Equal(HttpStatusCode.Forbidden, (await web.SendAsync(semCabecalho)).StatusCode);

        var comCabecalho = ComCookie(HttpMethod.Post, "/Campanha", cookie);
        comCabecalho.Headers.Add("X-Requested-With", "wyvern");
        comCabecalho.Content = JsonContent.Create(new { nome = "Mesa", sistema = "D&D 5.5e" });
        Assert.True((await web.SendAsync(comCabecalho)).IsSuccessStatusCode);

        // leitura não precisa do cabeçalho
        Assert.Equal(HttpStatusCode.OK, (await web.SendAsync(ComCookie(HttpMethod.Get, "/Campanha", cookie))).StatusCode);
    }

    [Fact]
    public async Task Bearer_continua_valendo_sem_o_cabecalho_anti_csrf()
    {
        var (nativo, _) = await _factory.NovoUsuarioAsync();

        var resposta = await nativo.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5.5e" });

        Assert.True(resposta.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Hub_conecta_so_com_o_cookie_sem_token_na_url()
    {
        var web = _factory.NovoClienteWeb();
        var cookie = ParCookie(await CadastrarAsync(web));

        var conexao = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/cenahub"), o =>
            {
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.Headers["Cookie"] = cookie;
            })
            .Build();

        await using (conexao)
        {
            await conexao.StartAsync();
            Assert.Equal(HubConnectionState.Connected, conexao.State);
        }
    }

    [Fact]
    public async Task Hub_recusa_origem_nao_permitida()
    {
        var web = _factory.NovoClienteWeb();
        var cookie = ParCookie(await CadastrarAsync(web));

        var estranha = ComCookie(HttpMethod.Post, "/cenahub/negotiate?negotiateVersion=1", cookie);
        estranha.Headers.Add("Origin", "https://site-malicioso.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await web.SendAsync(estranha)).StatusCode);

        var propria = ComCookie(HttpMethod.Post, "/cenahub/negotiate?negotiateVersion=1", cookie);
        propria.Headers.Add("Origin", "http://localhost:4200");
        Assert.Equal(HttpStatusCode.OK, (await web.SendAsync(propria)).StatusCode);
    }
}
