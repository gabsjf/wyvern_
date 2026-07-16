using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombateTracker3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Combates",
                columns: table => new
                {
                    CombateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessaoId = table.Column<int>(type: "int", nullable: false),
                    RodadaAtual = table.Column<int>(type: "int", nullable: false),
                    TurnoAtualIndex = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combates", x => x.CombateId);
                    table.ForeignKey(
                        name: "FK_Combates_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "SessaoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombateParticipantes",
                columns: table => new
                {
                    ParticipanteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CombateId = table.Column<int>(type: "int", nullable: false),
                    PersonagemId = table.Column<int>(type: "int", nullable: true),
                    NomeNPC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iniciativa = table.Column<int>(type: "int", nullable: false),
                    VidaAtual = table.Column<int>(type: "int", nullable: false),
                    VidaMaxima = table.Column<int>(type: "int", nullable: false),
                    ClasseArmadura = table.Column<int>(type: "int", nullable: false),
                    IsInimigo = table.Column<bool>(type: "bit", nullable: false),
                    Condicoes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombateParticipantes", x => x.ParticipanteId);
                    table.ForeignKey(
                        name: "FK_CombateParticipantes_Combates_CombateId",
                        column: x => x.CombateId,
                        principalTable: "Combates",
                        principalColumn: "CombateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombateParticipantes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombateParticipantes_CombateId",
                table: "CombateParticipantes",
                column: "CombateId");

            migrationBuilder.CreateIndex(
                name: "IX_CombateParticipantes_PersonagemId",
                table: "CombateParticipantes",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates",
                column: "SessaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombateParticipantes");

            migrationBuilder.DropTable(
                name: "Combates");
        }
    }
}
