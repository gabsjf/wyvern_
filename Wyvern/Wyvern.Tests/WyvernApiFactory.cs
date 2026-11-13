using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wyvern.Application.Services;

namespace Wyvern.Tests;

/// <summary>API real em memória, com SQLite em arquivo temporário e uma chave JWT só de teste.</summary>
public class WyvernApiFactory : WebApplicationFactory<Program>
{
    // Os testes cadastram muitos usuários do mesmo "IP"; o teste de rate limit usa um limite baixo.
    public int AuthPermitLimit { get; init; } = 1000;

    /// <summary>Nulo: vale o appsettings (beta ligado). Os testes de planos desligam para os limites valerem.</summary>
    public bool? Beta { get; init; }

    /// <summary>E-mail que recebe o claim de administrador.</summary>
    public string? AdminEmail { get; init; }

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"wyvern-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // UseSetting vale desde o início; ConfigureAppConfiguration chegaria depois de o Program.cs ler a chave JWT.
        if (PostgresTestes.ConnectionString is { } postgres)
        {
            // Mesmo caminho de produção: PostgreSQL + migrations.
            builder.UseSetting("Database:Provider", "Postgres");
            builder.UseSetting("ConnectionStrings:DefaultConnection", postgres);
        }
        else
        {
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        }
        builder.UseSetting("Jwt:Key", "chave-de-teste-com-mais-de-trinta-e-dois-caracteres");
        builder.UseSetting("RateLimit:AuthPermitLimit", AuthPermitLimit.ToString());
        if (Beta is { } beta) builder.UseSetting("Planos:Beta", beta ? "true" : "false");
        if (AdminEmail != null) builder.UseSetting("Admin:Emails:0", AdminEmail);
        builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(Emails));
    }

    /// <summary>Os e-mails que a API "enviou" durante os testes.</summary>
    public EmailSenderFalso Emails { get; } = new();

    public record EmailEnviado(string Para, string Assunto, string Corpo, string? Html = null);

    public class EmailSenderFalso : IEmailSender
    {
        public ConcurrentQueue<EmailEnviado> Enviados { get; } = new();

        public Task EnviarAsync(string para, string assunto, string corpo, string? corpoHtml = null)
        {
            Enviados.Enqueue(new EmailEnviado(para, assunto, corpo, corpoHtml));
            return Task.CompletedTask;
        }

        public IEnumerable<EmailEnviado> Para(string email) => Enviados.Where(e => e.Para == email);
    }

    /// <summary>Token do último link de confirmação enviado a este e-mail.</summary>
    public string TokenDeConfirmacao(string email)
    {
        var corpo = Emails.Para(email).Last(e => e.Assunto.StartsWith("Confirme")).Corpo;
        return Uri.UnescapeDataString(Regex.Match(corpo, @"token=(\S+)").Groups[1].Value);
    }

    /// <summary>Token do último link de redefinição de senha enviado a este e-mail.</summary>
    public string TokenDeRedefinicao(string email)
    {
        var corpo = Emails.Para(email).Last(e => e.Assunto.StartsWith("Redefinição")).Corpo;
        return Uri.UnescapeDataString(Regex.Match(corpo, @"token=(\S+)").Groups[1].Value);
    }

    /// <summary>Cadastra, confirma o e-mail pelo link e entra: devolve a resposta do login (com a sessão), como o fluxo real.</summary>
    public async Task<HttpResponseMessage> CadastrarEEntrarAsync(HttpClient client, string email, string nome = "Teste", string senha = "senha123")
    {
        var cadastro = await client.PostAsJsonAsync("/Auth/register", new { nome, email, senha });
        cadastro.EnsureSuccessStatusCode();
        var confirmacao = await client.PostAsJsonAsync("/Auth/confirmar-email", new { token = TokenDeConfirmacao(email) });
        confirmacao.EnsureSuccessStatusCode();
        return await client.PostAsJsonAsync("/Auth/login", new { email, senha });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch (IOException) { }
    }

    public record Sessao(int UsuarioId, string Token);

    /// <summary>
    /// Cliente como o navegador: guarda o cookie de sessão sozinho e não manda o cabeçalho de cliente nativo,
    /// então o token nunca aparece no corpo das respostas.
    /// </summary>
    public HttpClient NovoClienteWeb() =>
        CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>Cadastra um usuário novo e devolve um HttpClient já autenticado com o token dele (como o app nativo, que recebe o token no corpo).</summary>
    public async Task<(HttpClient client, Sessao sessao)> NovoUsuarioAsync()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var email = $"{Guid.NewGuid():N}@teste.com";
        var resposta = await CadastrarEEntrarAsync(client, email);
        resposta.EnsureSuccessStatusCode();

        var corpo = (await resposta.Content.ReadFromJsonAsync<Sessao>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.Token);
        return (client, corpo);
    }
}
