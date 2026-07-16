using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personagens_PersonagemPlayers_PersonagemPlayerPersonagemId",
                table: "Personagens");

            migrationBuilder.DropIndex(
                name: "IX_Personagens_PersonagemPlayerPersonagemId",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "PersonagemPlayerPersonagemId",
                table: "Personagens");

            migrationBuilder.InsertData(
                table: "TipoPersonagem",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Jogador" },
                    { 2, "NPC" },
                    { 3, "Monstro" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TipoPersonagem",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TipoPersonagem",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TipoPersonagem",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "PersonagemPlayerPersonagemId",
                table: "Personagens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_PersonagemPlayerPersonagemId",
                table: "Personagens",
                column: "PersonagemPlayerPersonagemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personagens_PersonagemPlayers_PersonagemPlayerPersonagemId",
                table: "Personagens",
                column: "PersonagemPlayerPersonagemId",
                principalTable: "PersonagemPlayers",
                principalColumn: "PersonagemId");
        }
    }
}
