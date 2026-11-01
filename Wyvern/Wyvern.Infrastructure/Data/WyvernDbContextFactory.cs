using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wyvern.Infrastructure.Data
{
    /// <summary>
    /// Usada só pelas ferramentas do EF (dotnet ef migrations ...). Não abre conexão: serve apenas
    /// para o EF conhecer o provedor (PostgreSQL) ao gerar migrations, sem subir a aplicação.
    /// </summary>
    public class WyvernDbContextFactory : IDesignTimeDbContextFactory<WyvernDbContext>
    {
        public WyvernDbContext CreateDbContext(string[] args)
        {
            // O banco existente foi criado com este comportamento (DateTime como "timestamp without time zone").
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var options = new DbContextOptionsBuilder<WyvernDbContext>()
                .UseNpgsql("Host=localhost;Database=wyvern_design_time;Username=design;Password=design")
                .Options;

            return new WyvernDbContext(options);
        }
    }
}
