using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPastaAnotacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Anotacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Anotacoes_Sessoes_SessaoId",
                table: "Anotacoes");

            migrationBuilder.RenameColumn(
                name: "SessaoId",
                table: "Anotacoes",
                newName: "CampanhaId");

            migrationBuilder.RenameIndex(
                name: "IX_Anotacoes_SessaoId",
                table: "Anotacoes",
                newName: "IX_Anotacoes_CampanhaId");

            migrationBuilder.AddColumn<int>(
                name: "PastaId",
                table: "Anotacoes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PastasAnotacao",
                columns: table => new
                {
                    PastaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampanhaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPublica = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastasAnotacao", x => x.PastaId);
                    table.ForeignKey(
                        name: "FK_PastasAnotacao_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anotacoes_PastaId",
                table: "Anotacoes",
                column: "PastaId");

            migrationBuilder.CreateIndex(
                name: "IX_PastasAnotacao_CampanhaId",
                table: "PastasAnotacao",
                column: "CampanhaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anotacoes_Campanhas_CampanhaId",
                table: "Anotacoes",
                column: "CampanhaId",
                principalTable: "Campanhas",
                principalColumn: "CampanhaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Anotacoes_PastasAnotacao_PastaId",
                table: "Anotacoes",
                column: "PastaId",
                principalTable: "PastasAnotacao",
                principalColumn: "PastaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Anotacoes_Campanhas_CampanhaId",
                table: "Anotacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Anotacoes_PastasAnotacao_PastaId",
                table: "Anotacoes");

            migrationBuilder.DropTable(
                name: "PastasAnotacao");

            migrationBuilder.DropIndex(
                name: "IX_Anotacoes_PastaId",
                table: "Anotacoes");

            migrationBuilder.DropColumn(
                name: "PastaId",
                table: "Anotacoes");

            migrationBuilder.RenameColumn(
                name: "CampanhaId",
                table: "Anotacoes",
                newName: "SessaoId");

            migrationBuilder.RenameIndex(
                name: "IX_Anotacoes_CampanhaId",
                table: "Anotacoes",
                newName: "IX_Anotacoes_SessaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anotacoes_Sessoes_SessaoId",
                table: "Anotacoes",
                column: "SessaoId",
                principalTable: "Sessoes",
                principalColumn: "SessaoId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
