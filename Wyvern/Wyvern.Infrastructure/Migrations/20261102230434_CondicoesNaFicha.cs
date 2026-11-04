using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CondicoesNaFicha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condicoes",
                table: "PersonagemCombates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condicoes",
                table: "PersonagemCombates");
        }
    }
}
