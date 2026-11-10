using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wyvern.Tests;

/// <summary>API com o beta desligado, para os limites dos planos valerem, e com um e-mail de administrador.</summary>
public class WyvernApiFactorySemBeta : WyvernApiFactory
{
    public const string EmailAdmin = "admin-planos@teste.com";

    public WyvernApiFactorySemBeta()
    {
        Beta = false;
        AdminEmail = EmailAdmin;
    }
}

/// <summary>
/// Tipos de conta e planos fora do beta: conta Jogador só entra por convite; Mestre Free cria 1 campanha com até 2
/// jogadores; o admin muda o plano; nada que já existe some quando o plano fica menor.
/// </summary>
public class PlanosTests : IClassFixture<WyvernApiFactorySemBeta>
{
    private readonly WyvernApiFactorySemBeta _factory;

    public PlanosTests(WyvernApiFactorySemBeta factory) => _factory = factory;

    private record Conta(string Tipo, string Plano, bool Beta, bool PodeCriarCampanha, int? MaxCampanhas, int? MaxJogadoresPorCampanha, int CampanhasCriadas);
    private record Perfil(Conta Conta);

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private Task<(HttpClient client, WyvernApiFactory.Sessao sessao)> MestreAsync() => _factory.NovoUsuarioAsync("Mestre");
    private Task<(HttpClient client, WyvernApiFactory.Sessao sessao)> JogadorAsync() => _factory.NovoUsuarioAsync("Jogador");

    private static Task<HttpResponseMessage> CriarCampanhaAsync(HttpClient client, string nome = "Mesa") =>
        client.PostAsJsonAsync("/Campanha", new { nome, sistema = "D&D 5e" });

    private static async Task<int> CriarCampanhaOkAsync(HttpClient client, string nome = "Mesa")
    {
        var resposta = await CriarCampanhaAsync(client, nome);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    private static async Task<string> ConviteAsync(HttpClient mestre, int campanhaId)
    {
        var resposta = await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static Task<HttpResponseMessage> EntrarAsync(HttpClient client, string token) =>
        client.PostAsync($"/Campanha/join/{token}", null);

    private static async Task<Conta> ContaAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<Perfil>("/Usuario/me", Web))!.Conta;

    // O admin é um e-mail fixo da configuração: cadastra na primeira vez e depois só entra.
    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var login = await client.PostAsJsonAsync("/Auth/login", new { email = WyvernApiFactorySemBeta.EmailAdmin, senha = "senha123" });
        if (!login.IsSuccessStatusCode)
            login = await _factory.CadastrarEEntrarAsync(client, WyvernApiFactorySemBeta.EmailAdmin);
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Catalogo_publico_diz_que_o_beta_esta_desligado_e_lista_os_planos()
    {
        var catalogo = await _factory.NovoClienteWeb().GetFromJsonAsync<JsonElement>("/Planos");

        Assert.False(catalogo.GetProperty("beta").GetBoolean());
        var chaves = catalogo.GetProperty("planos").EnumerateArray().Select(p => p.GetProperty("chave").GetString()).ToList();
        Assert.Contains("Free", chaves);
        Assert.Contains("Ultra", chaves);
    }

    [Fact]
    public async Task Fora_do_beta_papel_invalido_no_cadastro_vira_jogador()
    {
        var (_, sessao) = await _factory.NovoUsuarioAsync(papel: "Admin");
        Assert.Equal("Jogador", sessao.Papel);
    }

    [Fact]
    public async Task Conta_jogador_nao_cria_campanha_ate_virar_mestre()
    {
        var (jogador, _) = await JogadorAsync();

        var conta = await ContaAsync(jogador);
        Assert.Equal("Jogador", conta.Tipo);
        Assert.False(conta.PodeCriarCampanha);
        Assert.Equal(HttpStatusCode.Forbidden, (await CriarCampanhaAsync(jogador)).StatusCode);

        var virou = await jogador.PostAsync("/Usuario/me/virar-mestre", null);
        Assert.Equal(HttpStatusCode.OK, virou.StatusCode);

        conta = await ContaAsync(jogador);
        Assert.Equal("Mestre", conta.Tipo);
        Assert.Equal("Free", conta.Plano);
        Assert.Equal(HttpStatusCode.Created, (await CriarCampanhaAsync(jogador)).StatusCode);
    }

    [Fact]
    public async Task Mestre_free_cria_so_uma_campanha()
    {
        var (mestre, _) = await MestreAsync();

        Assert.Equal(HttpStatusCode.Created, (await CriarCampanhaAsync(mestre)).StatusCode);
        var segunda = await CriarCampanhaAsync(mestre, "Outra");

        Assert.Equal(HttpStatusCode.Forbidden, segunda.StatusCode);
        Assert.Contains("Free", await segunda.Content.ReadAsStringAsync());

        var conta = await ContaAsync(mestre);
        Assert.False(conta.PodeCriarCampanha);
        Assert.Equal(1, conta.CampanhasCriadas);
        Assert.Equal(1, conta.MaxCampanhas);
    }

    [Fact]
    public async Task Auth_me_avisa_se_pode_criar_campanha()
    {
        var (mestre, _) = await MestreAsync();
        Assert.True((await mestre.GetFromJsonAsync<JsonElement>("/Auth/me")).GetProperty("podeCriarCampanha").GetBoolean());

        await CriarCampanhaOkAsync(mestre);

        Assert.False((await mestre.GetFromJsonAsync<JsonElement>("/Auth/me")).GetProperty("podeCriarCampanha").GetBoolean());
    }

    [Fact]
    public async Task Campanha_free_aceita_dois_jogadores_e_recusa_o_terceiro()
    {
        var (mestre, _) = await MestreAsync();
        var campanhaId = await CriarCampanhaOkAsync(mestre);
        var token = await ConviteAsync(mestre, campanhaId);

        var (primeiro, _) = await JogadorAsync();
        var (segundo, _) = await JogadorAsync();
        var (terceiro, _) = await JogadorAsync();

        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(primeiro, token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(segundo, token)).StatusCode);

        var cheia = await EntrarAsync(terceiro, token);
        Assert.Equal(HttpStatusCode.BadRequest, cheia.StatusCode);
        Assert.Contains("cheia", await cheia.Content.ReadAsStringAsync());

        // quem já é membro não é barrado por abrir o convite de novo
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(primeiro, token)).StatusCode);
    }

    [Fact]
    public async Task Promover_um_jogador_a_co_mestre_nao_abre_vaga()
    {
        var (mestre, _) = await MestreAsync();
        var campanhaId = await CriarCampanhaOkAsync(mestre);
        var token = await ConviteAsync(mestre, campanhaId);

        var (primeiro, sessaoPrimeiro) = await JogadorAsync();
        var (segundo, _) = await JogadorAsync();
        await EntrarAsync(primeiro, token);
        await EntrarAsync(segundo, token);
        var promover = await mestre.PutAsJsonAsync($"/Campanha/{campanhaId}/membros/{sessaoPrimeiro.UsuarioId}/papel", new { papel = "Mestre" });
        Assert.True(promover.IsSuccessStatusCode);

        var (terceiro, _) = await JogadorAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await EntrarAsync(terceiro, token)).StatusCode);
    }

    [Fact]
    public async Task Mestre_entra_como_jogador_na_campanha_de_outra_pessoa()
    {
        var (dono, _) = await MestreAsync();
        var campanhaId = await CriarCampanhaOkAsync(dono);
        var token = await ConviteAsync(dono, campanhaId);

        var (outroMestre, _) = await MestreAsync();
        await CriarCampanhaOkAsync(outroMestre, "A minha"); // já usou a única vaga do Free

        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(outroMestre, token)).StatusCode);
        var campanha = await outroMestre.GetFromJsonAsync<JsonElement>($"/Campanha/{campanhaId}");
        Assert.Equal("Jogador", campanha.GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Admin_muda_o_plano_e_o_limite_sobe()
    {
        var (mestre, sessao) = await MestreAsync();
        await CriarCampanhaOkAsync(mestre);
        Assert.Equal(HttpStatusCode.Forbidden, (await CriarCampanhaAsync(mestre, "Segunda")).StatusCode);

        var admin = await AdminAsync();
        var mudou = await admin.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "standard" });
        Assert.Equal(HttpStatusCode.OK, mudou.StatusCode);

        var conta = await ContaAsync(mestre);
        Assert.Equal("Standard", conta.Plano);
        Assert.Equal(3, conta.MaxCampanhas);
        Assert.Equal(HttpStatusCode.Created, (await CriarCampanhaAsync(mestre, "Segunda")).StatusCode);
    }

    [Fact]
    public async Task Admin_tornar_jogador_em_mestre_ao_dar_um_plano()
    {
        var (jogador, sessao) = await JogadorAsync();
        var admin = await AdminAsync();

        await admin.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "Vip" });

        var conta = await ContaAsync(jogador);
        Assert.Equal("Mestre", conta.Tipo);
        Assert.Equal("Vip", conta.Plano);
    }

    [Fact]
    public async Task So_admin_muda_plano_e_plano_desconhecido_e_recusado()
    {
        var (mestre, sessao) = await MestreAsync();
        var naoAdmin = await mestre.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "Ultra" });
        Assert.Equal(HttpStatusCode.Forbidden, naoAdmin.StatusCode);

        var admin = await AdminAsync();
        var desconhecido = await admin.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "Platina" });
        Assert.Equal(HttpStatusCode.BadRequest, desconhecido.StatusCode);
        Assert.Equal("Free", (await ContaAsync(mestre)).Plano);
    }

    [Fact]
    public async Task Baixar_de_plano_mantem_as_campanhas_que_ja_existem()
    {
        var (mestre, sessao) = await MestreAsync();
        var admin = await AdminAsync();
        await admin.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "Standard" });
        var primeira = await CriarCampanhaOkAsync(mestre, "Primeira");
        var segunda = await CriarCampanhaOkAsync(mestre, "Segunda");

        await admin.PutAsJsonAsync($"/Admin/usuarios/{sessao.UsuarioId}/plano", new { plano = "Free" });

        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync($"/Campanha/{primeira}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await mestre.GetAsync($"/Campanha/{segunda}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CriarCampanhaAsync(mestre, "Terceira")).StatusCode);
        Assert.Equal(2, (await ContaAsync(mestre)).CampanhasCriadas);
    }

    [Fact]
    public async Task Jogador_na_campanha_so_cria_personagem_jogador()
    {
        var (mestre, _) = await MestreAsync();
        var campanhaId = await CriarCampanhaOkAsync(mestre);
        var (jogador, _) = await JogadorAsync();
        await EntrarAsync(jogador, await ConviteAsync(mestre, campanhaId));

        var criado = await jogador.PostAsJsonAsync("/Personagem", new
        {
            nome = "Quer ser NPC", campanhaId, tipoId = 2,
            personagemPlayer = new { classe = "Mago", nivel = 1, raca = "Elfo" },
            personagemCombate = new { vidaMaxima = 8, vidaAtual = 8, dadoVidaMaximo = "1d6" },
        });

        Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());
        Assert.Equal(1, (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tipoId").GetInt32());
    }
}

/// <summary>Com o beta ligado (o padrão do appsettings), toda conta é Mestre e não há limites.</summary>
public class PlanosNoBetaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public PlanosNoBetaTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public async Task No_beta_quem_se_cadastra_como_jogador_vira_mestre_sem_limites()
    {
        var (conta, _) = await _factory.NovoUsuarioAsync("Jogador");

        var perfil = await conta.GetFromJsonAsync<JsonElement>("/Usuario/me");
        Assert.Equal("Mestre", perfil.GetProperty("conta").GetProperty("tipo").GetString());
        Assert.True(perfil.GetProperty("conta").GetProperty("beta").GetBoolean());

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Created, (await conta.PostAsJsonAsync("/Campanha", new { nome = $"Mesa {i}", sistema = "D&D 5e" })).StatusCode);
    }

    [Fact]
    public async Task No_beta_a_mesa_aceita_mais_jogadores_que_o_free()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var criada = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        var campanhaId = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
        var token = (await (await mestre.PostAsync($"/Campanha/{campanhaId}/generate-invite", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        for (var i = 0; i < 4; i++)
        {
            var (jogador, _) = await _factory.NovoUsuarioAsync("Jogador");
            Assert.Equal(HttpStatusCode.OK, (await jogador.PostAsync($"/Campanha/join/{token}", null)).StatusCode);
        }
    }
}
