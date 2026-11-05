using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

/// <summary>
/// O WebSocket do app nativo não manda cabeçalho, então o token vai na URL. Para o JWT da sessão não parar em logs e
/// histórico, na URL só vale um ticket de ~60 s que serve apenas nos hubs.
/// </summary>
public class HubTicketTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public HubTicketTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<string> PedirTicketAsync(HttpClient client)
    {
        var resposta = await client.PostAsync("/Auth/hub-ticket", null);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ticket").GetString()!;
    }

    private static Task<HttpResponseMessage> NegociarNaUrlAsync(HttpClient client, string token) =>
        client.PostAsync($"/mesahub/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}", null);

    private static HttpRequestMessage ComBearer(HttpMethod metodo, string url, string token)
    {
        var pedido = new HttpRequestMessage(metodo, url);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return pedido;
    }

    [Fact]
    public async Task Pedir_ticket_sem_sessao_e_401()
    {
        var resposta = await _factory.NovoClienteWeb().PostAsync("/Auth/hub-ticket", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Ticket_na_url_do_hub_autentica()
    {
        var (cliente, _) = await _factory.NovoUsuarioAsync();
        var ticket = await PedirTicketAsync(cliente);

        var resposta = await NegociarNaUrlAsync(_factory.NovoClienteWeb(), ticket);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Ticket_no_cabecalho_do_hub_autentica()
    {
        var (cliente, _) = await _factory.NovoUsuarioAsync();
        var ticket = await PedirTicketAsync(cliente);

        var resposta = await _factory.NovoClienteWeb().SendAsync(ComBearer(HttpMethod.Post, "/mesahub/negotiate?negotiateVersion=1", ticket));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Jwt_da_sessao_na_url_do_hub_e_rejeitado()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync();

        var resposta = await NegociarNaUrlAsync(_factory.NovoClienteWeb(), sessao.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Jwt_da_sessao_no_cabecalho_do_hub_continua_valendo()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync();

        var resposta = await _factory.NovoClienteWeb().SendAsync(ComBearer(HttpMethod.Post, "/mesahub/negotiate?negotiateVersion=1", sessao.Token));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Ticket_nao_serve_na_api_rest_nem_para_gerar_outro_ticket()
    {
        var (cliente, _) = await _factory.NovoUsuarioAsync();
        var ticket = await PedirTicketAsync(cliente);
        var web = _factory.NovoClienteWeb();

        Assert.Equal(HttpStatusCode.Unauthorized, (await web.SendAsync(ComBearer(HttpMethod.Get, "/Auth/me", ticket))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await web.SendAsync(ComBearer(HttpMethod.Post, "/Auth/hub-ticket", ticket))).StatusCode);
    }

    [Fact]
    public async Task Ticket_leva_a_mesma_identidade_do_usuario()
    {
        var (cliente, sessao) = await _factory.NovoUsuarioAsync();
        var ticket = await PedirTicketAsync(cliente);

        // O ticket é um JWT: confere quem ele representa e que vence em poucos segundos, não horas.
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(ticket);

        Assert.Equal(sessao.UsuarioId.ToString(), jwt.Claims.Single(c => c.Type == "nameid").Value);
        Assert.Equal("1", jwt.Claims.Single(c => c.Type == "hub_ticket").Value);
        Assert.True(jwt.ValidTo - DateTime.UtcNow <= TimeSpan.FromSeconds(90));
    }
}
