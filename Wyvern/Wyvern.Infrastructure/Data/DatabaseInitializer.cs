using Microsoft.EntityFrameworkCore;
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
                return;
            }

            await AdotarBancoLegadoAsync(db, logger);
            await db.Database.MigrateAsync();
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
