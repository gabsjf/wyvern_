using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Wyvern.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        /// <summary>
        /// PostgreSQL (produção): aplica as migrations. SQLite (só desenvolvimento e testes): cria o schema direto,
        /// já que as migrations são específicas do PostgreSQL.
        /// </summary>
        public static async Task InitializeAsync(WyvernDbContext db, ILogger logger)
        {
            if (!db.Database.IsNpgsql())
            {
                await db.Database.EnsureCreatedAsync();
                await EvoluirSqliteAsync(db, logger);
                return;
            }

            await AdotarBancoLegadoAsync(db, logger);
            await db.Database.MigrateAsync();
        }

        /// <summary>
        /// EnsureCreated só cria um banco vazio: num banco SQLite que já existe ele não acrescenta o que o modelo ganhou
        /// depois. Aqui se cria as tabelas que faltam e se acrescentam as colunas novas, sem apagar nada. É só para o
        /// desenvolvimento local; em produção (PostgreSQL) quem faz isso são as migrations.
        /// </summary>
        private static async Task EvoluirSqliteAsync(WyvernDbContext db, ILogger logger)
        {
            var existentes = (await ConsultarAsync(db, "SELECT name FROM sqlite_master WHERE type = 'table'"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 1) tabelas novas (com seus índices), a partir do script que o próprio EF geraria
            var novas = db.Model.GetEntityTypes()
                .Select(t => t.GetTableName()).Where(n => n != null && !existentes.Contains(n!)).Select(n => n!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (novas.Count > 0)
            {
                foreach (var comando in Regex.Split(db.Database.GenerateCreateScript(), @";\s*\r?\n").Select(c => c.Trim()).Where(c => c.Length > 0))
                {
                    // CREATE TABLE "X" (...)  ou  CREATE [UNIQUE] INDEX "IX" ON "X" (...)
                    var tabela = Regex.Match(comando, @"^CREATE TABLE\s+""(?<t>[^""]+)""").Groups["t"].Value;
                    if (tabela.Length == 0)
                        tabela = Regex.Match(comando, @"^CREATE (?:UNIQUE )?INDEX\s+""[^""]+""\s+ON\s+""(?<t>[^""]+)""").Groups["t"].Value;

                    if (tabela.Length > 0 && novas.Contains(tabela))
                    {
                        logger.LogInformation("SQLite: criando {Tabela}", tabela);
                        await db.Database.ExecuteSqlRawAsync(comando);
                    }
                }
            }

            // 2) colunas novas em tabelas que já existiam
            foreach (var entidade in db.Model.GetEntityTypes())
            {
                var tabela = entidade.GetTableName();
                if (tabela == null || !existentes.Contains(tabela)) continue;

                var colunas = (await ConsultarAsync(db, $"SELECT name FROM pragma_table_info('{tabela}')"))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var propriedade in entidade.GetProperties())
                {
                    var coluna = propriedade.GetColumnName();
                    if (colunas.Contains(coluna)) continue;

                    var tipo = propriedade.GetColumnType();
                    // o SQLite exige um valor padrão para acrescentar uma coluna NOT NULL
                    var padrao = propriedade.IsNullable ? "" : propriedade.ClrType == typeof(string) ? " NOT NULL DEFAULT ''"
                        : propriedade.ClrType == typeof(DateTime) ? " NOT NULL DEFAULT '0001-01-01 00:00:00'" : " NOT NULL DEFAULT 0";
                    logger.LogInformation("SQLite: acrescentando a coluna {Tabela}.{Coluna}", tabela, coluna);
                    await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{tabela}\" ADD COLUMN \"{coluna}\" {tipo}{padrao}");
                }
            }
        }

        private static async Task<List<string>> ConsultarAsync(WyvernDbContext db, string sql)
        {
            var conexao = db.Database.GetDbConnection();
            var abriu = conexao.State != System.Data.ConnectionState.Open;
            if (abriu) await conexao.OpenAsync();
            try
            {
                await using var comando = conexao.CreateCommand();
                comando.CommandText = sql;
                var nomes = new List<string>();
                await using var leitor = await comando.ExecuteReaderAsync();
                while (await leitor.ReadAsync()) nomes.Add(leitor.GetString(0));
                return nomes;
            }
            finally
            {
                if (abriu) await conexao.CloseAsync();
            }
        }

        /// <summary>
        /// Bancos criados antes das migrations (via EnsureCreated) já têm as tabelas, mas não têm histórico.
        /// Sem este passo, a migration inicial tentaria recriar tudo e falharia. Aqui a migration inicial
        /// é registrada como já aplicada; as seguintes rodam normalmente em MigrateAsync.
        /// </summary>
        private static async Task AdotarBancoLegadoAsync(WyvernDbContext db, ILogger logger)
        {
            if ((await db.Database.GetAppliedMigrationsAsync()).Any())
                return;

            var temTabelas = await db.Database
                .SqlQuery<bool>($"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'Usuario') AS \"Value\"")
                .SingleAsync();

            if (!temTabelas)
                return; // banco novo: MigrateAsync cria tudo do zero

            var baseline = db.Database.GetMigrations().First();
            logger.LogWarning("Banco existente sem histórico de migrations: registrando {Migration} como já aplicada.", baseline);

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                );");

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ({0}, '9.0.0') ON CONFLICT DO NOTHING;",
                baseline);
        }
    }
}
