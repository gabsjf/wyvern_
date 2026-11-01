using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

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

        Assert.Equal("Jogador", sessao.Papel);
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
        Assert.Equal(HttpStatusCode.Forbidden, (await b.DeleteAsync($"/Usuario/{sessaoA.UsuarioId}")).StatusCode);
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
