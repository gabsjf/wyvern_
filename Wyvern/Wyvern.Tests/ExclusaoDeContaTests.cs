using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

/// <summary>
/// Exclusão de conta: pede senha e "EXCLUIR"; campanhas passam ao co-Mestre ou são encerradas; personagens ficam nas mesas;
/// anotações privadas somem e as públicas ficam sem autor; os dados pessoais somem e o e-mail fica livre.
/// </summary>
public class ExclusaoDeContaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public ExclusaoDeContaTests(WyvernApiFactory factory) => _factory = factory;

    private record Conta(HttpClient Client, int UsuarioId, string Email, string Token);
    private record Membro(int UsuarioId, string Nome, string Papel, bool EhCriador);

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private async Task<Conta> NovaContaAsync(string nome = "Aventureira")
    {
        var email = $"{Guid.NewGuid():N}@teste.com";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var login = await _factory.CadastrarEEntrarAsync(client, email, nome);
        login.EnsureSuccessStatusCode();
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = corpo.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new Conta(client, corpo.GetProperty("usuarioId").GetInt32(), email, token);
    }

    private static Task<HttpResponseMessage> ExcluirAsync(Conta conta, string senha = "senha123", string confirmacao = "EXCLUIR") =>
        conta.Client.PostAsJsonAsync("/Usuario/me/excluir", new { senha, confirmacao });

    private static async Task<int> CriarCampanhaAsync(Conta mestre, string nome = "Mesa")
    {
        var resposta = await mestre.Client.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5e" });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task EntrarNaCampanhaAsync(Conta mestre, int campanhaId, Conta jogador)
    {
        var convite = await mestre.Client.PostAsync($"/Campanha/{campanhaId}/generate-invite", null);
        var token = (await convite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        (await jogador.Client.PostAsync($"/Campanha/join/{token}", null)).EnsureSuccessStatusCode();
    }

    private static async Task PromoverAsync(Conta dono, int campanhaId, Conta membro) =>
        (await dono.Client.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{membro.UsuarioId}/papel", new { papel = "Mestre" })).EnsureSuccessStatusCode();

    private static async Task<List<Membro>> MembrosAsync(Conta quem, int campanhaId) =>
        (await quem.Client.GetFromJsonAsync<List<Membro>>($"/Campanha/{campanhaId}/membros", Web))!;

    private async Task<HttpStatusCode> LoginAsync(string email) =>
        (await _factory.CreateClient().PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" })).StatusCode;

    [Fact]
    public async Task Senha_errada_ou_sem_confirmacao_nao_exclui()
    {
        var conta = await NovaContaAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await ExcluirAsync(conta, senha: "errada")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ExcluirAsync(conta, confirmacao: "sim")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await conta.Client.GetAsync("/Usuario/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await LoginAsync(conta.Email));
    }

    [Fact]
    public async Task Excluir_derruba_as_sessoes_apaga_os_dados_e_libera_o_email()
    {
        var conta = await NovaContaAsync("Joana Tempestade");
        var outraSessao = await _factory.CreateClient().PostAsJsonAsync("/Auth/login", new { email = conta.Email, senha = "senha123" });
        Assert.Equal(HttpStatusCode.OK, outraSessao.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await ExcluirAsync(conta)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await conta.Client.GetAsync("/Auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(conta.Email));

        var aviso = _factory.Emails.Para(conta.Email).Last();
        Assert.StartsWith("A sua conta foi excluída", aviso.Assunto);

        // o mesmo e-mail pode criar uma conta nova do zero
        var nova = await _factory.CadastrarEEntrarAsync(_factory.CreateClient(), conta.Email, "De volta");
        Assert.Equal(HttpStatusCode.OK, nova.StatusCode);
        Assert.NotEqual(conta.UsuarioId, (await nova.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetInt32());
    }

    [Fact]
    public async Task Campanha_com_co_mestre_passa_para_ele()
    {
        var dona = await NovaContaAsync("Dona");
        var coMestre = await NovaContaAsync("Co-Mestre");
        var jogador = await NovaContaAsync("Jogador");
        var campanhaId = await CriarCampanhaAsync(dona, "Herdada");
        await EntrarNaCampanhaAsync(dona, campanhaId, coMestre);
        await EntrarNaCampanhaAsync(dona, campanhaId, jogador);
        await PromoverAsync(dona, campanhaId, coMestre);

        var previa = await dona.Client.GetFromJsonAsync<JsonElement>("/Usuario/me/exclusao");
        Assert.Equal("Co-Mestre", previa.GetProperty("transferidas")[0].GetProperty("novoDono").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await ExcluirAsync(dona)).StatusCode);

        var membros = await MembrosAsync(coMestre, campanhaId);
        Assert.DoesNotContain(membros, m => m.UsuarioId == dona.UsuarioId);
        Assert.True(membros.Single(m => m.UsuarioId == coMestre.UsuarioId).EhCriador);
        Assert.Equal(HttpStatusCode.OK, (await jogador.Client.GetAsync($"/Campanha/{campanhaId}")).StatusCode);
    }

    [Fact]
    public async Task Campanha_sem_co_mestre_e_encerrada_para_todos()
    {
        var dona = await NovaContaAsync();
        var jogador = await NovaContaAsync();
        var campanhaId = await CriarCampanhaAsync(dona, "Sozinha");
        await EntrarNaCampanhaAsync(dona, campanhaId, jogador);

        var previa = await dona.Client.GetFromJsonAsync<JsonElement>("/Usuario/me/exclusao");
        Assert.Equal("Sozinha", previa.GetProperty("encerradas")[0].GetString());

        await ExcluirAsync(dona);

        Assert.Equal(HttpStatusCode.NotFound, (await jogador.Client.GetAsync($"/Campanha/{campanhaId}")).StatusCode);
    }

    [Fact]
    public async Task Jogador_que_sai_deixa_o_personagem_na_mesa()
    {
        var mestre = await NovaContaAsync();
        var jogador = await NovaContaAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        await EntrarNaCampanhaAsync(mestre, campanhaId, jogador);
        var criado = await jogador.Client.PostAsJsonAsync("/Personagem", new
        {
            nome = "Thorin", campanhaId, tipoId = 1,
            personagemPlayer = new { classe = "Guerreiro", nivel = 1, raca = "Anão", antecedente = "Soldado", subclasse = "Campeão" },
            atributo = Fichas.Atributos,
            personagemCombate = new { classeArmadura = 14, vidaMaxima = 12, vidaAtual = 12, dadoVidaMaximo = "1d10", deslocamento = "9m" },
        });
        var personagemId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personagemId").GetInt32();

        await ExcluirAsync(jogador);

        Assert.Equal(HttpStatusCode.OK, (await mestre.Client.GetAsync($"/Personagem/{personagemId}")).StatusCode);
        Assert.DoesNotContain(await MembrosAsync(mestre, campanhaId), m => m.UsuarioId == jogador.UsuarioId);
    }

    [Fact]
    public async Task Anotacoes_privadas_somem_e_as_publicas_ficam_sem_autor()
    {
        var dona = await NovaContaAsync();
        var coMestre = await NovaContaAsync();
        var campanhaId = await CriarCampanhaAsync(dona);
        await EntrarNaCampanhaAsync(dona, campanhaId, coMestre);
        await PromoverAsync(dona, campanhaId, coMestre);

        async Task<int> AnotarAsync(bool publica)
        {
            var r = await dona.Client.PostAsJsonAsync("/Anotacao", new { campanhaId, titulo = publica ? "Mapa" : "Segredo", conteudo = "...", isPublica = publica });
            r.EnsureSuccessStatusCode();
            return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("anotacaoId").GetInt32();
        }
        var publica = await AnotarAsync(true);
        var privada = await AnotarAsync(false);

        await ExcluirAsync(dona);

        var lida = await coMestre.Client.GetAsync($"/Anotacao/{publica}");
        Assert.Equal(HttpStatusCode.OK, lida.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await lida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("criadoPorId").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await coMestre.Client.GetAsync($"/Anotacao/{privada}")).StatusCode);
    }

    [Fact]
    public async Task Unico_mestre_que_nao_era_dono_sai_e_o_dono_volta_a_ser_mestre()
    {
        var dona = await NovaContaAsync("Dona");
        var coMestre = await NovaContaAsync("Co-Mestre");
        var campanhaId = await CriarCampanhaAsync(dona);
        await EntrarNaCampanhaAsync(dona, campanhaId, coMestre);
        await PromoverAsync(dona, campanhaId, coMestre);
        // a dona passa a mesa e se rebaixa: o co-Mestre fica sendo o único Mestre
        (await dona.Client.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{dona.UsuarioId}/papel", new { papel = "Jogador" })).EnsureSuccessStatusCode();

        await ExcluirAsync(coMestre);

        Assert.Equal("Mestre", (await MembrosAsync(dona, campanhaId)).Single(m => m.UsuarioId == dona.UsuarioId).Papel);
        Assert.Equal(HttpStatusCode.OK, (await dona.Client.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).StatusCode);
    }

    [Fact]
    public async Task A_rota_antiga_por_id_nao_existe_mais()
    {
        var conta = await NovaContaAsync();
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await conta.Client.DeleteAsync($"/Usuario/{conta.UsuarioId}")).StatusCode);
    }
}
