using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Wyvern.Tests;

/// <summary>API real em memória, com SQLite em arquivo temporário e uma chave JWT só de teste.</summary>
public class WyvernApiFactory : WebApplicationFactory<Program>
{
    // Os testes cadastram muitos usuários do mesmo "IP"; o teste de rate limit usa um limite baixo.
    public int AuthPermitLimit { get; init; } = 1000;

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
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch (IOException) { }
    }

    public record Sessao(int UsuarioId, string Papel, string Token);

    /// <summary>
    /// Cliente como o navegador: guarda o cookie de sessão sozinho e não manda o cabeçalho de cliente nativo,
    /// então o token nunca aparece no corpo das respostas.
    /// </summary>
    public HttpClient NovoClienteWeb() =>
        CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>Cadastra um usuário novo e devolve um HttpClient já autenticado com o token dele (como o app nativo, que recebe o token no corpo).</summary>
    public async Task<(HttpClient client, Sessao sessao)> NovoUsuarioAsync(string? papel = "Mestre")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Wyvern-Client", "native");
        var email = $"{Guid.NewGuid():N}@teste.com";
        var resposta = await client.PostAsJsonAsync("/Auth/register", new { nome = "Teste", email, senha = "senha123", papel });
        resposta.EnsureSuccessStatusCode();

        var corpo = (await resposta.Content.ReadFromJsonAsync<Sessao>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.Token);
        return (client, corpo);
    }
}
