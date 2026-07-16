using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonagemAtaques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonagemAtaques",
                columns: table => new
                {
                    PersonagemAtaqueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Alcance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BonusAcerto = table.Column<int>(type: "int", nullable: false),
                    Dano = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDano = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Propriedades = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAtaques", x => x.PersonagemAtaqueId);
                    table.ForeignKey(
                        name: "FK_PersonagemAtaques_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemAtaques_PersonagemId",
                table: "PersonagemAtaques",
                column: "PersonagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonagemAtaques");
        }
    }
}
