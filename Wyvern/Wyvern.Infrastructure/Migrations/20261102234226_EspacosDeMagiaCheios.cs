using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EspacosDeMagiaCheios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes da ficha controlar o uso dos espaços de magia, "atual = 0" significava "não informado" e a tela mostrava
            // o máximo. Agora o valor é de verdade, então as fichas existentes passam a refletir o que elas já mostravam.
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel1Atual"" = ""SlotsNivel1Max"" WHERE ""SlotsNivel1Atual"" = 0 AND ""SlotsNivel1Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel2Atual"" = ""SlotsNivel2Max"" WHERE ""SlotsNivel2Atual"" = 0 AND ""SlotsNivel2Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel3Atual"" = ""SlotsNivel3Max"" WHERE ""SlotsNivel3Atual"" = 0 AND ""SlotsNivel3Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel4Atual"" = ""SlotsNivel4Max"" WHERE ""SlotsNivel4Atual"" = 0 AND ""SlotsNivel4Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel5Atual"" = ""SlotsNivel5Max"" WHERE ""SlotsNivel5Atual"" = 0 AND ""SlotsNivel5Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel6Atual"" = ""SlotsNivel6Max"" WHERE ""SlotsNivel6Atual"" = 0 AND ""SlotsNivel6Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel7Atual"" = ""SlotsNivel7Max"" WHERE ""SlotsNivel7Atual"" = 0 AND ""SlotsNivel7Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel8Atual"" = ""SlotsNivel8Max"" WHERE ""SlotsNivel8Atual"" = 0 AND ""SlotsNivel8Max"" > 0;");
            migrationBuilder.Sql(@"UPDATE ""PersonagemConjuracoes"" SET ""SlotsNivel9Atual"" = ""SlotsNivel9Max"" WHERE ""SlotsNivel9Atual"" = 0 AND ""SlotsNivel9Max"" > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
