using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VttDeslocamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DeslocamentoMetros",
                table: "TokensCena",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovimentoGasto",
                table: "TokensCena",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MovimentoTurno",
                table: "TokensCena",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LimitarDeslocamento",
                table: "Cenas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "MetrosPorQuadrado",
                table: "Cenas",
                type: "double precision",
                nullable: false,
                defaultValue: 1.5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeslocamentoMetros",
                table: "TokensCena");

            migrationBuilder.DropColumn(
                name: "MovimentoGasto",
                table: "TokensCena");

            migrationBuilder.DropColumn(
                name: "MovimentoTurno",
                table: "TokensCena");

            migrationBuilder.DropColumn(
                name: "LimitarDeslocamento",
                table: "Cenas");

            migrationBuilder.DropColumn(
                name: "MetrosPorQuadrado",
                table: "Cenas");
        }
    }
}
