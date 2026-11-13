using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MuraisDoMestre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Murais",
                columns: table => new
                {
                    MuralId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    CriadoPorId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Murais", x => x.MuralId);
                    table.ForeignKey(
                        name: "FK_Murais_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MuralImagens",
                columns: table => new
                {
                    MuralImagemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MuralId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuralImagens", x => x.MuralImagemId);
                    table.ForeignKey(
                        name: "FK_MuralImagens_Murais_MuralId",
                        column: x => x.MuralId,
                        principalTable: "Murais",
                        principalColumn: "MuralId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Murais_CampanhaId",
                table: "Murais",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_MuralImagens_MuralId",
                table: "MuralImagens",
                column: "MuralId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuralImagens");

            migrationBuilder.DropTable(
                name: "Murais");
        }
    }
}
