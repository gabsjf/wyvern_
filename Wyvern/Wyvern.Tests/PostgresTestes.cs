using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Tests;

/// <summary>
/// Testes contra um PostgreSQL real, ativados pela variÃ¡vel WYVERN_TEST_PG
/// (ex.: "Host=localhost;Database=wyverndb;Username=wyvern;Password=..."). Sem ela, a suÃ­te usa SQLite.
/// O banco apontado Ã© de testes: o conteÃºdo pode ser alterado.
/// </summary>
public static class PostgresTestes
{
    public static string? ConnectionString => Environment.GetEnvironmentVariable("WYVERN_TEST_PG");

    public static bool Disponivel => !string.IsNullOrWhiteSpace(ConnectionString);
}

public class MigrationsPostgresTests
{
    private const string Schema = "legacy_test";

    private static string ConnectionStringDoSchema()
    {
        var builder = new NpgsqlConnectionStringBuilder(PostgresTestes.ConnectionString!) { SearchPath = Schema };
        return builder.ConnectionString;
    }

    private static WyvernDbContext NovoContexto()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var options = new DbContextOptionsBuilder<WyvernDbContext>().UseNpgsql(ConnectionStringDoSchema()).Options;
        return new WyvernDbContext(options);
    }

    private static async Task ExecutarAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(PostgresTestes.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task<long> ContarAsync(WyvernDbContext db, string sql)
    {
        await using var conexao = new NpgsqlConnection(ConnectionStringDoSchema());
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    [SkippableFact]
    public async Task Banco_legado_com_duplicatas_e_adotado_e_limpo_pela_migration()
    {
        Skip.IfNot(PostgresTestes.Disponivel, "Defina WYVERN_TEST_PG para rodar contra PostgreSQL.");

        // Schema prÃ³prio: nÃ£o toca nas tabelas das outras suÃ­tes.
        await ExecutarAsync($"DROP SCHEMA IF EXISTS {Schema} CASCADE; CREATE SCHEMA {Schema};");

        // 1) Cria o schema ANTIGO (sÃ³ o Baseline) e apaga o histÃ³rico, como fazia o EnsureCreated.
        await using (var db = NovoContexto())
        {
            var baseline = db.Database.GetMigrations().First();
            await db.GetService<IMigrator>().MigrateAsync(baseline);
        }
        await ExecutarAsync($@"DROP TABLE {Schema}.""__EFMigrationsHistory"";");

        // 2) Dados sujos que o schema antigo permitia: jogador repetido e dois combates ativos na mesma sessÃ£o.
        await ExecutarAsync($@"
            INSERT INTO {Schema}.""Usuario"" (""Nome"", ""Email"", ""SenhaHash"", ""Papel"", ""CriadoEm"", ""Ativo"")
            VALUES ('M', 'm@t.com', 'x', 'Mestre', now(), true), ('J', 'j@t.com', 'x', 'Jogador', now(), true);
            INSERT INTO {Schema}.""Campanhas"" (""Nome"", ""Sistema"", ""MestreId"", ""Ativo"", ""CriadoEm"")
            SELECT 'C', 'S', ""UsuarioId"", true, now() FROM {Schema}.""Usuario"" WHERE ""Email"" = 'm@t.com';
            INSERT INTO {Schema}.""Campanhas_Jogadores"" (""CampanhaId"", ""UsuarioId"", ""EntrouEm"")
            SELECT c.""CampanhaId"", u.""UsuarioId"", now() FROM {Schema}.""Campanhas"" c, {Schema}.""Usuario"" u WHERE u.""Email"" = 'j@t.com';
            INSERT INTO {Schema}.""Campanhas_Jogadores"" (""CampanhaId"", ""UsuarioId"", ""EntrouEm"")
            SELECT c.""CampanhaId"", u.""UsuarioId"", now() FROM {Schema}.""Campanhas"" c, {Schema}.""Usuario"" u WHERE u.""Email"" = 'j@t.com';
            INSERT INTO {Schema}.""Sessoes"" (""NumeroSessao"", ""Nome"", ""DataSessao"", ""Obs"", ""CampanhaId"", ""Ativo"")
            SELECT 1, 'S1', now(), '', ""CampanhaId"", true FROM {Schema}.""Campanhas"";
            INSERT INTO {Schema}.""Combates"" (""SessaoId"", ""RodadaAtual"", ""TurnoAtualIndex"", ""Ativo"", ""CriadoEm"")
            SELECT ""SessaoId"", 1, 0, true, now() FROM {Schema}.""Sessoes"";
            INSERT INTO {Schema}.""Combates"" (""SessaoId"", ""RodadaAtual"", ""TurnoAtualIndex"", ""Ativo"", ""CriadoEm"")
            SELECT ""SessaoId"", 1, 0, true, now() FROM {Schema}.""Sessoes"";");

        // 3) InicializaÃ§Ã£o real da aplicaÃ§Ã£o: adota o banco legado e aplica a migration de integridade.
        await using (var db = NovoContexto())
        {
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);

            var aplicadas = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Equal(db.Database.GetMigrations().ToList(), aplicadas);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            Assert.Equal(1, await ContarAsync(db, $@"SELECT count(*) FROM {Schema}.""Campanhas_Jogadores"""));
            Assert.Equal(1, await ContarAsync(db, $@"SELECT count(*) FROM {Schema}.""Combates"" WHERE ""Ativo"" = true"));
            Assert.Equal(2, await ContarAsync(db, $@"SELECT count(*) FROM {Schema}.""Combates"""));
        }

        // 4) As restriÃ§Ãµes passam a valer de fato.
        await Assert.ThrowsAsync<PostgresException>(() => ExecutarAsync($@"
            INSERT INTO {Schema}.""Usuario"" (""Nome"", ""Email"", ""SenhaHash"", ""Papel"", ""CriadoEm"", ""Ativo"")
            VALUES ('Dup', 'm@t.com', 'x', 'Jogador', now(), true);"));

        // 5) Rodar de novo Ã© inofensivo.
        await using (var db = NovoContexto())
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);

        await ExecutarAsync($"DROP SCHEMA {Schema} CASCADE;");
    }

    [SkippableFact]
    public async Task Banco_vazio_recebe_todas_as_migrations()
    {
        Skip.IfNot(PostgresTestes.Disponivel, "Defina WYVERN_TEST_PG para rodar contra PostgreSQL.");

        await ExecutarAsync($"DROP SCHEMA IF EXISTS {Schema} CASCADE; CREATE SCHEMA {Schema};");

        await using (var db = NovoContexto())
        {
            await DatabaseInitializer.InitializeAsync(db, NullLogger.Instance);

            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(3, await ContarAsync(db, $@"SELECT count(*) FROM {Schema}.""TipoPersonagem"""));
        }

        await ExecutarAsync($"DROP SCHEMA {Schema} CASCADE;");
    }
}
