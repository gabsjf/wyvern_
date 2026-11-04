using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CronicaDaCampanha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosCampanha",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SessaoId = table.Column<int>(type: "integer", nullable: true),
                    RotuloData = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    SoMestre = table.Column<bool>(type: "boolean", nullable: false),
                    Automatico = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoPorId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosCampanha", x => x.EventoId);
                    table.ForeignKey(
                        name: "FK_EventosCampanha_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventosCampanha_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "SessaoId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventosCampanha_Usuario_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosCampanha_CampanhaId_Ordem",
                table: "EventosCampanha",
                columns: new[] { "CampanhaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosCampanha_CriadoPorId",
                table: "EventosCampanha",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCampanha_SessaoId",
                table: "EventosCampanha",
                column: "SessaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosCampanha");
        }
    }
}
