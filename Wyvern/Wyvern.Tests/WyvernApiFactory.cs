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
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", "chave-de-teste-com-mais-de-trinta-e-dois-caracteres");
        builder.UseSetting("RateLimit:AuthPermitLimit", AuthPermitLimit.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch (IOException) { }
    }

    public record Sessao(int UsuarioId, string Papel, string Token);

    /// <summary>Cadastra um usuário novo e devolve um HttpClient já autenticado com o token dele.</summary>
    public async Task<(HttpClient client, Sessao sessao)> NovoUsuarioAsync(string? papel = "Mestre")
    {
        var client = CreateClient();
        var email = $"{Guid.NewGuid():N}@teste.com";
        var resposta = await client.PostAsJsonAsync("/Auth/register", new { nome = "Teste", email, senha = "senha123", papel });
        resposta.EnsureSuccessStatusCode();

        var corpo = (await resposta.Content.ReadFromJsonAsync<Sessao>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.Token);
        return (client, corpo);
    }
}
