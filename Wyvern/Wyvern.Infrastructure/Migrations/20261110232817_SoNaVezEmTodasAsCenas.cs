using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoNaVezEmTodasAsCenas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WYV-28: no combate, jogador só move na própria vez. As cenas novas já nascem assim; as antigas passam a ser.
            migrationBuilder.Sql(@"UPDATE ""Cenas"" SET ""SoNoTurno"" = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não há como saber quais cenas estavam desligadas antes; o Mestre pode desligar de novo no ajuste da cena.
        }
    }
}
