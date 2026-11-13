using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

public class MembrosTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public MembrosTests(WyvernApiFactory factory) => _factory = factory;

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre, string nome = "Mesa")
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarAsync(HttpClient mestre, HttpClient jogador, int campanhaId)
    {
        var convite = await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>();
        (await jogador.PostAsync($"/Campanha/join/{convite.GetProperty("token").GetString()}", null)).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> MembrosAsync(HttpClient client, int campanhaId) =>
        await client.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}/membros");

    private static string PapelDe(JsonElement membros, int usuarioId) =>
        membros.EnumerateArray().First(m => m.GetProperty("usuarioId").GetInt32() == usuarioId).GetProperty("papel").GetString()!;

    [Fact]
    public async Task Papel_e_por_campanha_o_mesmo_usuario_e_Mestre_numa_e_Jogador_noutra()
    {
        var (ana, sessaoAna) = await _factory.NovoUsuarioAsync();
        var (bia, _) = await _factory.NovoUsuarioAsync();
        var campanhaDaAna = await CriarCampanhaAsync(ana, "Da Ana");
        var campanhaDaBia = await CriarCampanhaAsync(bia, "Da Bia");
        await EntrarAsync(bia, ana, campanhaDaBia);

        var lista = await ana.GetFromJsonAsync<JsonElement>("/Campanha");
        string PapelNa(int id) => lista.EnumerateArray().First(c => c.GetProperty("campanhaId").GetInt32() == id).GetProperty("papel").GetString()!;
        Assert.Equal("Mestre", PapelNa(campanhaDaAna));
        Assert.Equal("Jogador", PapelNa(campanhaDaBia));

        // Mestre na própria campanha, sem poder nenhum na outra.
        Assert.Equal(HttpStatusCode.OK, (await ana.PutAsJsonAsync($"/Campanha/{campanhaDaAna}", new { nome = "Renomeada", sistema = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PutAsJsonAsync($"/Campanha/{campanhaDaBia}", new { nome = "Invadida", sistema = "x" })).StatusCode);
        Assert.Equal("Mestre", (await ana.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaDaAna}")).GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Lista_de_campanhas_nao_expoe_o_token_de_convite()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        var lista = await jogador.GetStringAsync("/Campanha");

        Assert.DoesNotContain("tokenConvite", lista, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mestre_promove_jogador_a_co_Mestre_e_o_novo_Mestre_passa_a_ter_poder()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        var novaSessao = new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId };
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.PostAsJsonAsync("/Sessao", novaSessao)).StatusCode);

        var promover = await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{sessaoJogador.UsuarioId}/papel", new { papel = "Mestre" });
        Assert.Equal(HttpStatusCode.NoContent, promover.StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await jogador.PostAsJsonAsync("/Sessao", novaSessao)).StatusCode);
        Assert.Equal("Mestre", PapelDe(await MembrosAsync(jogador, campanhaId), sessaoJogador.UsuarioId));
    }

    [Fact]
    public async Task Jogador_nao_altera_papeis_nem_remove_outros()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        // Tenta se promover e expulsar o Mestre
        Assert.Equal(HttpStatusCode.Forbidden,
            (await jogador.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{sessaoJogador.UsuarioId}/papel", new { papel = "Mestre" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/membros/{sessaoMestre.UsuarioId}")).StatusCode);
    }

    [Fact]
    public async Task Campanha_nunca_fica_sem_Mestre()
    {
        var (mestre, sessaoMestre) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        // O único Mestre não pode se rebaixar nem sair
        Assert.Equal(HttpStatusCode.BadRequest,
            (await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{sessaoMestre.UsuarioId}/papel", new { papel = "Jogador" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/membros/{sessaoMestre.UsuarioId}")).StatusCode);

        // Com outro Mestre, pode sair
        await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{sessaoJogador.UsuarioId}/papel", new { papel = "Mestre" });
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/Campanha/{campanhaId}/membros/{sessaoMestre.UsuarioId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await mestre.GetAsync($"/Campanha/{campanhaId}")).StatusCode);
    }

    [Fact]
    public async Task Jogador_pode_sair_e_perde_o_acesso()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);
        Assert.Equal(HttpStatusCode.OK, (await jogador.GetAsync($"/Campanha/{campanhaId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await jogador.DeleteAsync($"/Campanha/{campanhaId}/membros/{sessaoJogador.UsuarioId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await jogador.GetAsync($"/Campanha/{campanhaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await jogador.GetAsync($"/Campanha/{campanhaId}/membros")).StatusCode);
    }

    [Fact]
    public async Task Quem_saiu_nao_acessa_mais_os_personagens_que_criou()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (jogador, sessaoJogador) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, jogador, campanhaId);

        var criado = await jogador.PostAsJsonAsync("/Personagem", Fichas.Heroi("Herói", campanhaId));
        criado.EnsureSuccessStatusCode();
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await jogador.GetAsync($"/Personagem/{personagemId}")).StatusCode);

        await jogador.DeleteAsync($"/Campanha/{campanhaId}/membros/{sessaoJogador.UsuarioId}");

        Assert.NotEqual(HttpStatusCode.OK, (await jogador.GetAsync($"/Personagem/{personagemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync($"/Personagem/{personagemId}")).StatusCode);
    }

    [Fact]
    public async Task Pasta_pode_ser_editada_pelo_autor_e_pelo_Mestre_mas_nao_por_outro_jogador()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var (autor, _) = await _factory.NovoUsuarioAsync();
        var (outro, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarAsync(mestre, autor, campanhaId);
        await EntrarAsync(mestre, outro, campanhaId);

        var criada = await autor.PostAsJsonAsync("/PastaAnotacao", new { campanhaId, nome = "Minhas notas", isPublica = false });
        var pastaId = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("pastaId").GetInt32();
        var corpo = new { campanhaId, nome = "Renomeada", isPublica = false };

        Assert.Equal(HttpStatusCode.Forbidden, (await outro.PutAsJsonAsync($"/PastaAnotacao/{pastaId}", corpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outro.DeleteAsync($"/PastaAnotacao/{pastaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await autor.PutAsJsonAsync($"/PastaAnotacao/{pastaId}", corpo)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.DeleteAsync($"/PastaAnotacao/{pastaId}")).StatusCode);
    }
}
