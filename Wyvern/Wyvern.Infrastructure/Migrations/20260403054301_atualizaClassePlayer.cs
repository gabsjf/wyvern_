using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class atualizaClassePlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
