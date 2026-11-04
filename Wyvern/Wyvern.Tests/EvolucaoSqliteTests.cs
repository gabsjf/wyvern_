using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// O SQLite de desenvolvimento nasce com EnsureCreated, que não acrescenta nada a um banco que já existe.
/// A inicialização precisa completar o que o modelo ganhou depois, sem perder os dados.
/// </summary>
public class EvolucaoSqliteTests : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"wyvern-evolucao-{Guid.NewGuid():N}.db");

    private WyvernDbContext NovoContexto()
    {
        var opcoes = new DbContextOptionsBuilder<WyvernDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        return new WyvernDbContext(opcoes);
    }

    private static async Task ExecutarAsync(WyvernDbContext db, string sql) => await db.Database.ExecuteSqlRawAsync(sql);

    private static async Task<List<string>> ConsultarAsync(WyvernDbContext db, string sql)
    {
        var conexao = db.Database.GetDbConnection();
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        var itens = new List<string>();
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) itens.Add(leitor.GetString(0));
        return itens;
    }

    private static Task<List<string>> ColunasAsync(WyvernDbContext db, string tabela) =>
        ConsultarAsync(db, $"SELECT name FROM pragma_table_info('{tabela}')");

    private static Task<List<string>> IndicesAsync(WyvernDbContext db, string tabela) =>
        ConsultarAsync(db, $"SELECT name FROM pragma_index_list('{tabela}')");

    [Fact]
    public async Task Coluna_que_o_modelo_ganhou_e_acrescentada_sem_perder_dados()
    {
        // um banco "antigo": criado pelo modelo atual, mas sem a coluna Condicoes
        await using (var db = NovoContexto())
        {
            await db.Database.EnsureCreatedAsync();
            db.Usuarios.Add(new Wyvern.Domain.Entities.Usuario { Nome = "Ana", Email = "ana@teste.com", SenhaHash = "x", CriadoEm = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await ExecutarAsync(db, "ALTER TABLE \"PersonagemCombates\" DROP COLUMN \"Condicoes\"");
            Assert.DoesNotContain("Condicoes", await ColunasAsync(db, "PersonagemCombates"));
        }
        SqliteConnection.ClearAllPools();

        await using (var db = NovoContexto())
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);
        SqliteConnection.ClearAllPools();

        await using (var db = NovoContexto())
        {
            Assert.Contains("Condicoes", await ColunasAsync(db, "PersonagemCombates"));
            Assert.Equal(1, await db.Usuarios.CountAsync()); // os dados continuam lá
        }
    }

    [Fact]
    public async Task Tabela_que_o_modelo_ganhou_e_criada_com_seus_indices_sem_perder_dados()
    {
        // um banco "antigo": sem a tabela da crônica
        await using (var db = NovoContexto())
        {
            await db.Database.EnsureCreatedAsync();
            db.Usuarios.Add(new Wyvern.Domain.Entities.Usuario { Nome = "Ana", Email = "ana@teste.com", SenhaHash = "x", CriadoEm = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await ExecutarAsync(db, "DROP TABLE \"EventosCampanha\"");
            Assert.DoesNotContain("EventosCampanha", await ConsultarAsync(db, "SELECT name FROM sqlite_master WHERE type = 'table'"));
        }
        SqliteConnection.ClearAllPools();

        await using (var db = NovoContexto())
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);
        SqliteConnection.ClearAllPools();

        await using (var db = NovoContexto())
        {
            Assert.Contains("Titulo", await ColunasAsync(db, "EventosCampanha"));
            Assert.Contains(await IndicesAsync(db, "EventosCampanha"), i => i.Contains("CampanhaId"));
            Assert.Equal(0, await db.EventosCampanha.CountAsync()); // a tabela nova funciona
            Assert.Equal(1, await db.Usuarios.CountAsync()); // os dados que já existiam continuam lá
        }
    }

    [Fact]
    public async Task Rodar_a_inicializacao_de_novo_nao_muda_nada()
    {
        await using (var db = NovoContexto())
        {
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);
            Assert.Contains("Condicoes", await ColunasAsync(db, "PersonagemCombates"));
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_arquivo); } catch (IOException) { }
    }
}
