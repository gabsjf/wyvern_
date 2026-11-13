using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoverPapelDoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Papel",
                table: "Usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Papel",
                table: "Usuario",
                type: "text",
                nullable: false,
                defaultValue: "Mestre"); // o tipo de conta saiu na WYV-09; voltar recria a coluna com todo mundo Mestre, como no beta
        }
    }
}
