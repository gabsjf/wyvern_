using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>Relógio que só anda quando o teste manda.</summary>
internal sealed class RelogioFalso : TimeProvider
{
    private DateTimeOffset _agora = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _agora;
    public void Avancar(TimeSpan tempo) => _agora += tempo;
}

/// <summary>Regras do limitador: 5 erros abrem um bloqueio de 30 s que dobra a cada nova falha, até 15 min, e sempre passa.</summary>
public class LimitadorDeLoginTests
{
    private readonly RelogioFalso _relogio = new();
    private readonly LimitadorDeLogin _limitador;

    public LimitadorDeLoginTests()
    {
        _limitador = new LimitadorDeLogin(new MemoryCache(new MemoryCacheOptions()), _relogio, new ConfigurationBuilder().Build());
    }

    private void Falhar(string email, int vezes)
    {
        for (var i = 0; i < vezes; i++) _limitador.RegistrarFalha(email);
    }

    [Fact]
    public void Abaixo_do_limite_nao_bloqueia()
    {
        Falhar("a@x.com", 4);

        Assert.Null(_limitador.TempoRestanteDoBloqueio("a@x.com"));
    }

    [Fact]
    public void Quinta_falha_bloqueia_por_30_segundos()
    {
        Falhar("a@x.com", 5);

        Assert.Equal(TimeSpan.FromSeconds(30), _limitador.TempoRestanteDoBloqueio("a@x.com"));
    }

    [Fact]
    public void O_bloqueio_acaba_sozinho_com_o_tempo()
    {
        Falhar("a@x.com", 5);

        _relogio.Avancar(TimeSpan.FromSeconds(29));
        Assert.NotNull(_limitador.TempoRestanteDoBloqueio("a@x.com"));
        _relogio.Avancar(TimeSpan.FromSeconds(2));
        Assert.Null(_limitador.TempoRestanteDoBloqueio("a@x.com"));
    }

    [Fact]
    public void Cada_nova_falha_depois_do_bloqueio_dobra_o_tempo_ate_o_teto_de_15_minutos()
    {
        var esperado = new[] { 30, 60, 120, 240, 480, 900, 900, 900 };
        Falhar("a@x.com", 4);

        foreach (var segundos in esperado)
        {
            _limitador.RegistrarFalha("a@x.com");
            Assert.Equal(TimeSpan.FromSeconds(segundos), _limitador.TempoRestanteDoBloqueio("a@x.com"));
            _relogio.Avancar(TimeSpan.FromSeconds(segundos + 1)); // espera o bloqueio acabar e erra de novo
        }
    }

    [Fact]
    public void Senha_certa_zera_o_contador()
    {
        Falhar("a@x.com", 5);
        _limitador.Zerar("a@x.com");

        Assert.Null(_limitador.TempoRestanteDoBloqueio("a@x.com"));
        Falhar("a@x.com", 4); // recomeça: quatro erros ainda não bloqueiam
        Assert.Null(_limitador.TempoRestanteDoBloqueio("a@x.com"));
    }

    [Fact]
    public void Depois_de_30_minutos_sem_falhar_o_historico_e_esquecido()
    {
        Falhar("a@x.com", 4);

        _relogio.Avancar(TimeSpan.FromMinutes(31));
        _limitador.RegistrarFalha("a@x.com"); // seria a quinta, mas o contador recomeçou

        Assert.Null(_limitador.TempoRestanteDoBloqueio("a@x.com"));
    }

    [Fact]
    public void Contas_sao_independentes_e_o_email_ignora_maiusculas_e_espacos()
    {
        Falhar("Ana@X.com ", 5);

        Assert.NotNull(_limitador.TempoRestanteDoBloqueio("ana@x.com"));
        Assert.Null(_limitador.TempoRestanteDoBloqueio("bia@x.com"));
    }

    [Fact]
    public void Limites_vem_da_configuracao()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Login:TentativasAntesDoBloqueio"] = "2",
            ["Login:BloqueioInicialSegundos"] = "10",
            ["Login:BloqueioMaximoSegundos"] = "25",
        }).Build();
        var limitador = new LimitadorDeLogin(new MemoryCache(new MemoryCacheOptions()), _relogio, config);

        limitador.RegistrarFalha("a@x.com");
        Assert.Null(limitador.TempoRestanteDoBloqueio("a@x.com"));
        limitador.RegistrarFalha("a@x.com");
        Assert.Equal(TimeSpan.FromSeconds(10), limitador.TempoRestanteDoBloqueio("a@x.com"));
        _relogio.Avancar(TimeSpan.FromSeconds(11));
        limitador.RegistrarFalha("a@x.com");
        _relogio.Avancar(TimeSpan.FromSeconds(21));
        limitador.RegistrarFalha("a@x.com");
        Assert.Equal(TimeSpan.FromSeconds(25), limitador.TempoRestanteDoBloqueio("a@x.com")); // 40 s travado no teto
    }
}

/// <summary>O limitador na API: o 429 com Retry-After, o mesmo comportamento para e-mail inexistente e a saída por senha certa ou redefinição.</summary>
public class BloqueioDeLoginTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public BloqueioDeLoginTests(WyvernApiFactory factory) => _factory = factory;

    private static string NovoEmail() => $"{Guid.NewGuid():N}@teste.com";

    private static Task<HttpResponseMessage> Entrar(HttpClient client, string email, string senha) =>
        client.PostAsJsonAsync("/Auth/login", new { email, senha });

    private static async Task Errar(HttpClient client, string email, int vezes)
    {
        for (var i = 0; i < vezes; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(client, email, "senha-errada")).StatusCode);
    }

    [Fact]
    public async Task Cinco_senhas_erradas_bloqueiam_ate_a_senha_certa_com_429_e_retry_after()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);

        await Errar(client, email, 5);
        var bloqueada = await Entrar(client, email, "senha123"); // a senha certa também fica barrada durante o bloqueio

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.Contains("Muitas tentativas", await bloqueada.Content.ReadAsStringAsync());
        var retry = int.Parse(bloqueada.Headers.GetValues("Retry-After").Single());
        Assert.InRange(retry, 1, 30);
    }

    [Fact]
    public async Task Email_que_nao_existe_e_bloqueado_do_mesmo_jeito()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail(); // sem conta

        await Errar(client, email, 5);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Entrar(client, email, "qualquer")).StatusCode);
    }

    [Fact]
    public async Task Senha_certa_antes_do_limite_zera_o_contador()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);

        await Errar(client, email, 4);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "senha123")).StatusCode);
        await Errar(client, email, 4); // recomeça do zero: ainda não bloqueia

        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "senha123")).StatusCode);
    }

    [Fact]
    public async Task Bloqueio_de_uma_conta_nao_atrapalha_outra()
    {
        var client = _factory.NovoClienteWeb();
        var atacada = NovoEmail();
        var outra = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, atacada);
        await _factory.CadastrarEEntrarAsync(client, outra);

        await Errar(client, atacada, 5);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Entrar(client, atacada, "senha123")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, outra, "senha123")).StatusCode);
    }

    [Fact]
    public async Task Redefinir_a_senha_pelo_email_desfaz_o_bloqueio()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        await Errar(client, email, 5);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Entrar(client, email, "senha123")).StatusCode);

        await client.PostAsJsonAsync("/Auth/esqueci-senha", new { email });
        var token = _factory.TokenDeRedefinicao(email);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/Auth/redefinir-senha", new { token, novaSenha = "nova-senha-1" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Entrar(client, email, "nova-senha-1")).StatusCode);
    }

    [Fact]
    public async Task Pedir_o_link_de_redefinicao_nao_depende_do_login_estar_liberado()
    {
        var client = _factory.NovoClienteWeb();
        var email = NovoEmail();
        await _factory.CadastrarEEntrarAsync(client, email);
        await Errar(client, email, 5);

        var resposta = await client.PostAsJsonAsync("/Auth/esqueci-senha", new { email });

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
    }
}
