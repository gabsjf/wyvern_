using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

public class AutorizacaoTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public AutorizacaoTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var resposta = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa do A", sistema = "D&D 5e" });
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("campanhaId").GetInt32();
    }

    [Fact]
    public async Task Cadastro_ignora_papel_invalido_enviado_pelo_cliente()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync(papel: "Admin");

        // No beta toda conta é Mestre; fora dele o papel inválido vira Jogador (PlanosTests).
        Assert.Equal("Mestre", sessao.Papel);
    }

    [Fact]
    public async Task Rotas_exigem_token()
    {
        var anonimo = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Usuario/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Sessao")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Campanha")).StatusCode);
    }

    [Fact]
    public async Task Usuario_nao_acessa_nem_apaga_a_conta_de_outro()
    {
        var (a, sessaoA) = await _factory.NovoUsuarioAsync();
        var (b, _) = await _factory.NovoUsuarioAsync();

        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/Usuario/{sessaoA.UsuarioId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync($"/Usuario/{sessaoA.UsuarioId}")).StatusCode);
        // Excluir conta só existe como "me" (com senha), então não há rota por id para mirar a conta de outra pessoa.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await b.DeleteAsync($"/Usuario/{sessaoA.UsuarioId}")).StatusCode);
    }

    [Fact]
    public async Task Estranho_nao_exclui_nem_edita_campanha_de_outro_mestre()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        // Para quem não participa, a campanha simplesmente não existe.
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.DeleteAsync($"/Campanha/{campanhaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await estranho.PutAsJsonAsync($"/Campanha/{campanhaId}", new { nome = "invadida", sistema = "x" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync($"/Campanha/{campanhaId}")).StatusCode);
    }

    [Fact]
    public async Task Jogador_da_campanha_nao_pode_excluir_nem_criar_sessao()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync(papel: "Mestre"); // papel global não concede nada
        var campanhaId = await CriarCampanhaAsync(mestre);

        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var entrada = await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null);
        entrada.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}")).StatusCode);

        var novaSessao = new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId };
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync("/Sessao", novaSessao)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await mestre.PostAsJsonAsync("/Sessao", novaSessao)).StatusCode);
    }

    [Fact]
    public async Task Sessoes_de_outra_campanha_nao_aparecem_nem_sao_legiveis()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var criada = await mestre.PostAsJsonAsync("/Sessao",
            new { numeroSessao = 1, nome = "Segredo", dataSessao = DateTime.Today, obs = "", campanhaId });
        var sessaoId = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();

        var lista = await estranho.GetFromJsonAsync<JsonElement>("/Sessao");
        Assert.Equal(0, lista.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.GetAsync($"/Sessao/{sessaoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await estranho.DeleteAsync($"/Sessao/{sessaoId}")).StatusCode);
    }

    [Fact]
    public async Task Estranho_nao_le_anotacoes_nem_pastas_da_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        Assert.Equal(HttpStatusCode.Forbidden, (await estranho.GetAsync($"/Anotacao/campanha/{campanhaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await estranho.GetAsync($"/PastaAnotacao/campanha/{campanhaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await estranho.PostAsJsonAsync("/PastaAnotacao", new { campanhaId, nome = "x", isPublica = true })).StatusCode);
    }

    [Fact]
    public async Task Estranho_nao_cria_personagem_em_campanha_alheia()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        var resposta = await estranho.PostAsJsonAsync("/Personagem", new { nome = "Intruso", campanhaId, tipoId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Estranho_nao_inicia_nem_le_combate_de_outra_campanha()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (estranho, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await estranho.PostAsJsonAsync("/Combate/start", new { campanhaId, participantes = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await estranho.GetAsync($"/Combate/active/{campanhaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await mestre.PostAsJsonAsync("/Combate/start", new { participantes = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task Escrita_no_catalogo_global_exige_admin()
    {
        var (usuario, _) = await _factory.NovoUsuarioAsync();

        var resposta = await usuario.PostAsJsonAsync("/Item", new { nome = "Espada", descricao = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}

public class ConfiguracaoJwtTests
{
    [Fact]
    public void Aplicacao_nao_sobe_sem_chave_jwt()
    {
        var fabrica = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("environment", "Production"));

        Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
    }
}

public class RateLimitTests
{
    [Fact]
    public async Task Login_e_limitado_por_ip()
    {
        using var fabrica = new WyvernApiFactory { AuthPermitLimit = 3 };
        var client = fabrica.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 5; i++)
            statuses.Add((await client.PostAsJsonAsync("/Auth/login", new { email = "x@y.com", senha = "errada" })).StatusCode);

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }
}

/// <summary>
/// Autorização do fluxo de sessão: o cookie de acesso (ausente, expirado, adulterado), o refresh reutilizado, o logout que
/// precisa valer também para quem copiou os cookies antes, quem manda quando há cabeçalho e cookie, e os hubs sem credencial.
/// </summary>
public class AutorizacaoDeSessaoTests : IClassFixture<WyvernApiFactory>
{
    private const string ChaveDeTeste = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres";
    private const string Hub = "/cenahub/negotiate?negotiateVersion=1";

    private readonly WyvernApiFactory _factory;

    public AutorizacaoDeSessaoTests(WyvernApiFactory factory) => _factory = factory;

    /// <summary>Os dois cookies da sessão web, no formato "nome=valor" que o navegador reenviaria.</summary>
    private record Cookies(string Acesso, string Refresh)
    {
        public string Ambos => $"{Acesso}; {Refresh}";
        public string ValorAcesso => Acesso["wyvern_at=".Length..];
        public string ValorRefresh => Refresh["wyvern_rt=".Length..];
    }

    private static string Par(HttpResponseMessage resposta, string nome) =>
        resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(nome + "=", StringComparison.Ordinal)).Split(';')[0];

    private static Cookies LerCookies(HttpResponseMessage resposta) => new(Par(resposta, "wyvern_at"), Par(resposta, "wyvern_rt"));

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private async Task<Cookies> EntrarNaWebAsync(string email)
    {
        var login = await _factory.CadastrarEEntrarAsync(_factory.NovoClienteWeb(), email, "Sessao");
        login.EnsureSuccessStatusCode();
        return LerCookies(login);
    }

    /// <summary>Outro login do mesmo usuário (outro aparelho): outra família de refresh tokens.</summary>
    private async Task<Cookies> LogarDeNovoAsync(string email)
    {
        var login = await _factory.NovoClienteWeb().PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" });
        login.EnsureSuccessStatusCode();
        return LerCookies(login);
    }

    private Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, string? cookie = null, bool anticsrf = false, string? bearer = null)
    {
        var pedido = new HttpRequestMessage(metodo, url);
        if (cookie != null) pedido.Headers.Add("Cookie", cookie);
        if (anticsrf) pedido.Headers.Add("X-Requested-With", "wyvern");
        if (bearer != null) pedido.Headers.Authorization = new("Bearer", bearer);
        return _factory.NovoClienteWeb().SendAsync(pedido);
    }

    private async Task<HttpStatusCode> RotaAsync(string cookie) => (await EnviarAsync(HttpMethod.Get, "/Campanha", cookie)).StatusCode;
    private async Task<HttpStatusCode> HubAsync(string cookie) => (await EnviarAsync(HttpMethod.Post, Hub, cookie)).StatusCode;
    private Task<HttpResponseMessage> RenovarAsync(Cookies c) => EnviarAsync(HttpMethod.Post, "/Auth/refresh", c.Refresh, anticsrf: true);

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Refaz o JWT de uma sessão real com outro vencimento ou outra chave, para isolar o que está sendo testado.</summary>
    private static string Forjar(string jwtReal, DateTime expira, string chave = ChaveDeTeste)
    {
        var original = new JwtSecurityTokenHandler().ReadJwtToken(jwtReal);
        var token = new JwtSecurityToken(
            issuer: original.Issuer, audience: original.Audiences.Single(), claims: original.Claims,
            notBefore: expira.AddHours(-1), expires: expira,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Base64Url(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // ---------- cookie ausente ----------

    [Theory]
    [InlineData("GET", "/Campanha")]
    [InlineData("GET", "/Sessao")]
    [InlineData("GET", "/Auth/me")]
    [InlineData("POST", "/Auth/hub-ticket")]
    [InlineData("POST", "/combathub/negotiate?negotiateVersion=1")]
    [InlineData("POST", "/mesahub/negotiate?negotiateVersion=1")]
    [InlineData("POST", "/cenahub/negotiate?negotiateVersion=1")]
    public async Task Sem_cookie_nem_token_rotas_e_hubs_respondem_401(string metodo, string url)
    {
        var resposta = await EnviarAsync(new HttpMethod(metodo), url);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Cookie_valido_e_aceito_nas_rotas_e_nos_hubs()
    {
        var cookies = await EntrarNaWebAsync(NovoEmail()); // controle: sem isto os testes de recusa abaixo poderiam passar à toa

        Assert.Equal(HttpStatusCode.OK, await RotaAsync(cookies.Acesso));
        Assert.Equal(HttpStatusCode.OK, await HubAsync(cookies.Acesso));
    }

    // ---------- cookie expirado, adulterado ou forjado ----------

    [Theory]
    [InlineData("expirado")]
    [InlineData("assinatura-adulterada")]
    [InlineData("outra-chave")]
    [InlineData("alg-none")]
    [InlineData("lixo")]
    [InlineData("vazio")]
    public async Task Cookie_de_acesso_invalido_e_recusado_nas_rotas_e_nos_hubs(string tipo)
    {
        var real = (await EntrarNaWebAsync(NovoEmail())).ValorAcesso;
        var partes = real.Split('.');
        var assinaturaTrocada = partes[2][..5] + (partes[2][5] == 'A' ? 'B' : 'A') + partes[2][6..];
        var token = tipo switch
        {
            "expirado" => Forjar(real, DateTime.UtcNow.AddHours(-2)), // chave certa e sessão ativa: só o vencimento falha
            "assinatura-adulterada" => $"{partes[0]}.{partes[1]}.{assinaturaTrocada}",
            "outra-chave" => Forjar(real, DateTime.UtcNow.AddHours(1), "outra-chave-qualquer-com-mais-de-trinta-e-dois-caracteres"),
            "alg-none" => $"{Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{partes[1]}.",
            "lixo" => "isto-nao-e-um-jwt",
            _ => ""
        };
        var cookie = $"wyvern_at={token}";

        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync(cookie));
        Assert.Equal(HttpStatusCode.Unauthorized, await HubAsync(cookie));
    }

    [Fact]
    public async Task Cookie_de_acesso_vencido_com_refresh_valido_volta_a_funcionar_depois_do_refresh()
    {
        var cookies = await EntrarNaWebAsync(NovoEmail());
        var vencido = $"wyvern_at={Forjar(cookies.ValorAcesso, DateTime.UtcNow.AddHours(-2))}";

        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync($"{vencido}; {cookies.Refresh}")); // o refresh não autentica a rota
        var renovada = await EnviarAsync(HttpMethod.Post, "/Auth/refresh", $"{vencido}; {cookies.Refresh}", anticsrf: true);

        Assert.Equal(HttpStatusCode.OK, renovada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await RotaAsync(Par(renovada, "wyvern_at")));
    }

    [Fact]
    public async Task Refresh_token_no_lugar_do_cookie_de_acesso_nao_autentica()
    {
        var cookies = await EntrarNaWebAsync(NovoEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync($"wyvern_at={cookies.ValorRefresh}"));
    }

    [Fact]
    public async Task Ticket_de_hub_no_cookie_so_vale_nos_hubs()
    {
        var cookies = await EntrarNaWebAsync(NovoEmail());
        var resposta = await EnviarAsync(HttpMethod.Post, "/Auth/hub-ticket", cookies.Acesso, anticsrf: true);
        var ticket = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ticket").GetString()!;

        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync($"wyvern_at={ticket}"));
        Assert.Equal(HttpStatusCode.OK, await HubAsync($"wyvern_at={ticket}"));
    }

    // ---------- cabeçalho x cookie ----------

    [Fact]
    public async Task Cabecalho_authorization_vence_o_cookie_e_o_cookie_nao_serve_de_reserva()
    {
        var cookieDeA = (await EntrarNaWebAsync(NovoEmail())).Acesso;
        var (_, sessaoB) = await _factory.NovoUsuarioAsync();

        var comoB = await EnviarAsync(HttpMethod.Get, "/Auth/me", cookieDeA, bearer: sessaoB.Token);
        var cabecalhoInvalido = await EnviarAsync(HttpMethod.Get, "/Auth/me", cookieDeA, bearer: "token-invalido");

        Assert.Equal(sessaoB.UsuarioId, (await comoB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, cabecalhoInvalido.StatusCode); // cabeçalho ruim não cai para o cookie válido
    }

    // ---------- refresh reutilizado ----------

    [Fact]
    public async Task Reuso_do_refresh_na_web_derruba_a_familia_nas_rotas_de_negocio_e_poupa_outro_aparelho()
    {
        var email = NovoEmail();
        var aparelho1 = await EntrarNaWebAsync(email);
        var aparelho2 = await LogarDeNovoAsync(email);
        var renovada = await RenovarAsync(aparelho1);
        var atuais = LerCookies(renovada);
        Assert.Equal(HttpStatusCode.OK, await RotaAsync(atuais.Acesso));

        using (var escopo = _factory.Services.CreateScope()) // o refresh antigo foi trocado há mais que a tolerância
        {
            var db = escopo.ServiceProvider.GetRequiredService<WyvernDbContext>();
            var hash = Hash(aparelho1.ValorRefresh);
            (await db.RefreshTokens.SingleAsync(t => t.TokenHash == hash)).UsadoEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        var reuso = await RenovarAsync(aparelho1); // quem copiou o refresh antigo tenta usá-lo

        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync(atuais.Acesso)); // o acesso legítimo da mesma família cai junto
        Assert.Equal(HttpStatusCode.Unauthorized, await HubAsync(atuais.Acesso));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RenovarAsync(atuais)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await RotaAsync(aparelho2.Acesso)); // outro login do mesmo usuário não é atingido
    }

    // ---------- logout ----------

    [Fact]
    public async Task Logout_na_web_invalida_os_cookies_copiados_antes()
    {
        var cookies = await EntrarNaWebAsync(NovoEmail());
        Assert.Equal(HttpStatusCode.OK, await RotaAsync(cookies.Acesso));

        var logout = await EnviarAsync(HttpMethod.Post, "/Auth/logout", cookies.Ambos, anticsrf: true);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        // o navegador apagou os cookies, mas quem os copiou antes continua sem acesso
        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync(cookies.Acesso));
        Assert.Equal(HttpStatusCode.Unauthorized, await HubAsync(cookies.Acesso));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RenovarAsync(cookies)).StatusCode);
    }

    [Fact]
    public async Task Logout_de_um_aparelho_nao_derruba_o_outro_do_mesmo_usuario()
    {
        var email = NovoEmail();
        var aparelho1 = await EntrarNaWebAsync(email);
        var aparelho2 = await LogarDeNovoAsync(email);

        await EnviarAsync(HttpMethod.Post, "/Auth/logout", aparelho1.Ambos, anticsrf: true);

        Assert.Equal(HttpStatusCode.Unauthorized, await RotaAsync(aparelho1.Acesso));
        Assert.Equal(HttpStatusCode.OK, await RotaAsync(aparelho2.Acesso));
        Assert.Equal(HttpStatusCode.OK, await HubAsync(aparelho2.Acesso));
        Assert.Equal(HttpStatusCode.OK, (await RenovarAsync(aparelho2)).StatusCode);
    }

    [Fact]
    public async Task Logout_sem_sessao_ou_com_refresh_invalido_e_idempotente()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await EnviarAsync(HttpMethod.Post, "/Auth/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EnviarAsync(HttpMethod.Post, "/Auth/logout", "wyvern_rt=lixo", anticsrf: true)).StatusCode);
    }

    // ---------- hubs sem token na URL ----------

    [Theory]
    [InlineData("/combathub")]
    [InlineData("/mesahub")]
    [InlineData("/cenahub")]
    public async Task Cliente_signalr_sem_credencial_nao_conecta(string hub)
    {
        var conexao = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, hub), o =>
            {
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await using (conexao)
            await Assert.ThrowsAnyAsync<Exception>(() => conexao.StartAsync());
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("")]
    public async Task Access_token_invalido_na_url_do_hub_e_recusado(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, $"{Hub}&access_token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Access_token_na_query_de_rota_rest_nao_autentica()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync();

        var resposta = await EnviarAsync(HttpMethod.Get, $"/Campanha?access_token={Uri.EscapeDataString(sessao.Token)}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode); // a query só vale nos hubs, e lá só para ticket
    }
}
