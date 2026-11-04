using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VttParedesRetratoTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImagemVersao",
                table: "TokensCena",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessaoId",
                table: "Cenas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SoNoTurno",
                table: "Cenas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ParedesCena",
                columns: table => new
                {
                    ParedeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    X1 = table.Column<int>(type: "integer", nullable: false),
                    Y1 = table.Column<int>(type: "integer", nullable: false),
                    X2 = table.Column<int>(type: "integer", nullable: false),
                    Y2 = table.Column<int>(type: "integer", nullable: false),
                    Porta = table.Column<bool>(type: "boolean", nullable: false),
                    Aberta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParedesCena", x => x.ParedeId);
                    table.ForeignKey(
                        name: "FK_ParedesCena_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenImagens",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenImagens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_TokenImagens_TokensCena_TokenId",
                        column: x => x.TokenId,
                        principalTable: "TokensCena",
                        principalColumn: "TokenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cenas_SessaoId",
                table: "Cenas",
                column: "SessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParedesCena_CenaId",
                table: "ParedesCena",
                column: "CenaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cenas_Sessoes_SessaoId",
                table: "Cenas",
                column: "SessaoId",
                principalTable: "Sessoes",
                principalColumn: "SessaoId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cenas_Sessoes_SessaoId",
                table: "Cenas");

            migrationBuilder.DropTable(
                name: "ParedesCena");

            migrationBuilder.DropTable(
                name: "TokenImagens");

            migrationBuilder.DropIndex(
                name: "IX_Cenas_SessaoId",
                table: "Cenas");

            migrationBuilder.DropColumn(
                name: "ImagemVersao",
                table: "TokensCena");

            migrationBuilder.DropColumn(
                name: "SessaoId",
                table: "Cenas");

            migrationBuilder.DropColumn(
                name: "SoNoTurno",
                table: "Cenas");
        }
    }
}
