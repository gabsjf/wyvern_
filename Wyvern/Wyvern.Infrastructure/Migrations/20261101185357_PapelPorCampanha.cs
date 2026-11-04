using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <summary>
    /// Papel por campanha: Campanhas_Jogadores vira Campanhas_Membros (com Papel). O EF gerou DropTable + CreateTable,
    /// o que apagaria os jogadores existentes; aqui os dados são copiados antes de a tabela antiga sair.
    /// </summary>
    public partial class PapelPorCampanha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CriadoPorId",
                table: "PastasAnotacao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConviteExpiraEm",
                table: "Campanhas",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Campanhas_Membros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Papel = table.Column<int>(type: "integer", nullable: false),
                    EntrouEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas_Membros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campanhas_Membros_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Campanhas_Membros_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Jogadores atuais continuam jogadores (Papel 2 = Jogador).
            migrationBuilder.Sql(@"
                INSERT INTO ""Campanhas_Membros"" (""CampanhaId"", ""UsuarioId"", ""Papel"", ""EntrouEm"")
                SELECT ""CampanhaId"", ""UsuarioId"", 2, ""EntrouEm"" FROM ""Campanhas_Jogadores"";");

            // Quem criou cada campanha passa a constar como Mestre (Papel 1). Se já constava como jogador, é promovido.
            migrationBuilder.Sql(@"
                UPDATE ""Campanhas_Membros"" m SET ""Papel"" = 1
                FROM ""Campanhas"" c
                WHERE c.""CampanhaId"" = m.""CampanhaId"" AND c.""MestreId"" = m.""UsuarioId"";");

            migrationBuilder.Sql(@"
                INSERT INTO ""Campanhas_Membros"" (""CampanhaId"", ""UsuarioId"", ""Papel"", ""EntrouEm"")
                SELECT c.""CampanhaId"", c.""MestreId"", 1, c.""CriadoEm"" FROM ""Campanhas"" c
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Campanhas_Membros"" m
                    WHERE m.""CampanhaId"" = c.""CampanhaId"" AND m.""UsuarioId"" = c.""MestreId"");");

            migrationBuilder.DropTable(
                name: "Campanhas_Jogadores");

            migrationBuilder.CreateIndex(
                name: "IX_PastasAnotacao_CriadoPorId",
                table: "PastasAnotacao",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Membros_CampanhaId_UsuarioId",
                table: "Campanhas_Membros",
                columns: new[] { "CampanhaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Membros_UsuarioId",
                table: "Campanhas_Membros",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_PastasAnotacao_Usuario_CriadoPorId",
                table: "PastasAnotacao",
                column: "CriadoPorId",
                principalTable: "Usuario",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PastasAnotacao_Usuario_CriadoPorId",
                table: "PastasAnotacao");

            migrationBuilder.DropIndex(
                name: "IX_PastasAnotacao_CriadoPorId",
                table: "PastasAnotacao");

            migrationBuilder.DropColumn(
                name: "CriadoPorId",
                table: "PastasAnotacao");

            migrationBuilder.DropColumn(
                name: "ConviteExpiraEm",
                table: "Campanhas");

            migrationBuilder.CreateTable(
                name: "Campanhas_Jogadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    EntrouEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas_Jogadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campanhas_Jogadores_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Campanhas_Jogadores_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            // O modelo antigo só conhecia jogadores; os Mestres voltam a ser identificados por Campanhas.MestreId.
            migrationBuilder.Sql(@"
                INSERT INTO ""Campanhas_Jogadores"" (""CampanhaId"", ""UsuarioId"", ""EntrouEm"")
                SELECT m.""CampanhaId"", m.""UsuarioId"", m.""EntrouEm"" FROM ""Campanhas_Membros"" m
                JOIN ""Campanhas"" c ON c.""CampanhaId"" = m.""CampanhaId""
                WHERE m.""UsuarioId"" <> c.""MestreId"";");

            migrationBuilder.DropTable(
                name: "Campanhas_Membros");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Jogadores_CampanhaId_UsuarioId",
                table: "Campanhas_Jogadores",
                columns: new[] { "CampanhaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_Jogadores_UsuarioId",
                table: "Campanhas_Jogadores",
                column: "UsuarioId");
        }
    }
}
