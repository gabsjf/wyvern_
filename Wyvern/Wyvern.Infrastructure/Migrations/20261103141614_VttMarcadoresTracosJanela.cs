using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VttMarcadoresTracosJanela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Janela",
                table: "ParedesCena",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MarcadoresCena",
                columns: table => new
                {
                    MarcadorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    Coluna = table.Column<int>(type: "integer", nullable: false),
                    Linha = table.Column<int>(type: "integer", nullable: false),
                    Icone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nota = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Visivel = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcadoresCena", x => x.MarcadorId);
                    table.ForeignKey(
                        name: "FK_MarcadoresCena_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TracosCena",
                columns: table => new
                {
                    TracoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Cor = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Espessura = table.Column<int>(type: "integer", nullable: false),
                    Pontos = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracosCena", x => x.TracoId);
                    table.ForeignKey(
                        name: "FK_TracosCena_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TracosCena_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarcadoresCena_CenaId",
                table: "MarcadoresCena",
                column: "CenaId");

            migrationBuilder.CreateIndex(
                name: "IX_TracosCena_CenaId",
                table: "TracosCena",
                column: "CenaId");

            migrationBuilder.CreateIndex(
                name: "IX_TracosCena_UsuarioId",
                table: "TracosCena",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarcadoresCena");

            migrationBuilder.DropTable(
                name: "TracosCena");

            migrationBuilder.DropColumn(
                name: "Janela",
                table: "ParedesCena");
        }
    }
}
