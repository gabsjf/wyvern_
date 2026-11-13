using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

public class PersistenciaTests : IClassFixture<WyvernApiFactory>
{
    private readonly WyvernApiFactory _factory;

    public PersistenciaTests(WyvernApiFactory factory) => _factory = factory;

    [Fact]
    public void Migrations_cobrem_todo_o_modelo()
    {
        // Falha quando alguém altera uma entidade/configuração e esquece de gerar a migration correspondente.
        using var db = new WyvernDbContextFactory().CreateDbContext(Array.Empty<string>());

        Assert.False(db.Database.HasPendingModelChanges(),
            "O modelo mudou sem migration. Rode: dotnet ef migrations add <Nome> --project Wyvern.Infrastructure");
    }

    [Fact]
    public void Migrations_comecam_pelo_baseline_do_banco_legado()
    {
        using var db = new WyvernDbContextFactory().CreateDbContext(Array.Empty<string>());

        Assert.EndsWith("_Baseline", db.Database.GetMigrations().First());
    }

    private static async Task<int> CriarCampanhaAsync(HttpClient mestre)
    {
        var r = await mestre.PostAsJsonAsync("/Campanha", new { nome = "Mesa", sistema = "D&D 5e" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campanhaId").GetInt32();
    }

    [Fact]
    public async Task Nao_inicia_segundo_combate_ativo_na_mesma_sessao()
    {
        var (mestre, _) = await _factory.NovoUsuarioAsync();
        var campanhaId = await CriarCampanhaAsync(mestre);
        var sessao = await mestre.PostAsJsonAsync("/Sessao",
            new { numeroSessao = 1, nome = "S1", dataSessao = DateTime.Today, obs = "", campanhaId });
        var sessaoId = (await sessao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessaoId").GetInt32();
        var corpo = new { sessaoId, participantes = Array.Empty<object>() };

        var primeiro = await mestre.PostAsJsonAsync("/Combate/start", corpo);
        var segundo = await mestre.PostAsJsonAsync("/Combate/start", corpo);

        Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);

        // Depois de encerrar, pode iniciar outro.
        var combateId = (await primeiro.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("combateId").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await mestre.PostAsync($"/Combate/{combateId}/end", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await mestre.PostAsJsonAsync("/Combate/start", corpo)).StatusCode);
    }

    [Fact]
    public async Task Email_nao_pode_ser_cadastrado_duas_vezes()
    {
        var client = _factory.NovoClienteWeb();
        var email = $"{Guid.NewGuid():N}@teste.com";
        Assert.Equal(HttpStatusCode.OK, (await _factory.CadastrarEEntrarAsync(client, email)).StatusCode);

        // Cadastrar de novo não cria outra conta: a senha nova não serve e a antiga continua valendo.
        await client.PostAsJsonAsync("/Auth/register", new { nome = "Outro", email, senha = "outra-senha" });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "outra-senha" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/Auth/login", new { email, senha = "senha123" })).StatusCode);
    }
}
