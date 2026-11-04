using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VttMapaTatico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cenas",
                columns: table => new
                {
                    CenaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Largura = table.Column<int>(type: "integer", nullable: false),
                    Altura = table.Column<int>(type: "integer", nullable: false),
                    TamanhoCelula = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    ImagemVersao = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cenas", x => x.CenaId);
                    table.ForeignKey(
                        name: "FK_Cenas_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CenaImagens",
                columns: table => new
                {
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenaImagens", x => x.CenaId);
                    table.ForeignKey(
                        name: "FK_CenaImagens_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokensCena",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Coluna = table.Column<int>(type: "integer", nullable: false),
                    Linha = table.Column<int>(type: "integer", nullable: false),
                    Tamanho = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Inimigo = table.Column<bool>(type: "boolean", nullable: false),
                    Oculto = table.Column<bool>(type: "boolean", nullable: false),
                    PersonagemId = table.Column<int>(type: "integer", nullable: true),
                    ParticipanteId = table.Column<int>(type: "integer", nullable: true),
                    ControladoPorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensCena", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_TokensCena_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TokensCena_CombateParticipantes_ParticipanteId",
                        column: x => x.ParticipanteId,
                        principalTable: "CombateParticipantes",
                        principalColumn: "ParticipanteId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TokensCena_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TokensCena_Usuario_ControladoPorId",
                        column: x => x.ControladoPorId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cenas_CampanhaId_Ordem",
                table: "Cenas",
                columns: new[] { "CampanhaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_TokensCena_CenaId",
                table: "TokensCena",
                column: "CenaId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensCena_ControladoPorId",
                table: "TokensCena",
                column: "ControladoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensCena_ParticipanteId",
                table: "TokensCena",
                column: "ParticipanteId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensCena_PersonagemId",
                table: "TokensCena",
                column: "PersonagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CenaImagens");

            migrationBuilder.DropTable(
                name: "TokensCena");

            migrationBuilder.DropTable(
                name: "Cenas");
        }
    }
}
