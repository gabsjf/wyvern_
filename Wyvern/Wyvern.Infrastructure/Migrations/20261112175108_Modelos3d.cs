using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modelos3d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "TokensCena",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "Personagens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Modelos3d",
                columns: table => new
                {
                    ModeloId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    EnviadoPorId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modelos3d", x => x.ModeloId);
                    table.ForeignKey(
                        name: "FK_Modelos3d_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Modelos3d_Usuario_EnviadoPorId",
                        column: x => x.EnviadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Modelos3dArquivos",
                columns: table => new
                {
                    ModeloId = table.Column<int>(type: "integer", nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modelos3dArquivos", x => x.ModeloId);
                    table.ForeignKey(
                        name: "FK_Modelos3dArquivos_Modelos3d_ModeloId",
                        column: x => x.ModeloId,
                        principalTable: "Modelos3d",
                        principalColumn: "ModeloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjetosCena",
                columns: table => new
                {
                    ObjetoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CenaId = table.Column<int>(type: "integer", nullable: false),
                    ModeloId = table.Column<int>(type: "integer", nullable: false),
                    Coluna = table.Column<int>(type: "integer", nullable: false),
                    Linha = table.Column<int>(type: "integer", nullable: false),
                    Tamanho = table.Column<int>(type: "integer", nullable: false),
                    Rotacao = table.Column<int>(type: "integer", nullable: false),
                    Oculto = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjetosCena", x => x.ObjetoId);
                    table.ForeignKey(
                        name: "FK_ObjetosCena_Cenas_CenaId",
                        column: x => x.CenaId,
                        principalTable: "Cenas",
                        principalColumn: "CenaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjetosCena_Modelos3d_ModeloId",
                        column: x => x.ModeloId,
                        principalTable: "Modelos3d",
                        principalColumn: "ModeloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokensCena_ModeloId",
                table: "TokensCena",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_ModeloId",
                table: "Personagens",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_Modelos3d_CampanhaId_Categoria",
                table: "Modelos3d",
                columns: new[] { "CampanhaId", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_Modelos3d_EnviadoPorId",
                table: "Modelos3d",
                column: "EnviadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetosCena_CenaId",
                table: "ObjetosCena",
                column: "CenaId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetosCena_ModeloId",
                table: "ObjetosCena",
                column: "ModeloId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personagens_Modelos3d_ModeloId",
                table: "Personagens",
                column: "ModeloId",
                principalTable: "Modelos3d",
                principalColumn: "ModeloId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokensCena_Modelos3d_ModeloId",
                table: "TokensCena",
                column: "ModeloId",
                principalTable: "Modelos3d",
                principalColumn: "ModeloId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personagens_Modelos3d_ModeloId",
                table: "Personagens");

            migrationBuilder.DropForeignKey(
                name: "FK_TokensCena_Modelos3d_ModeloId",
                table: "TokensCena");

            migrationBuilder.DropTable(
                name: "Modelos3dArquivos");

            migrationBuilder.DropTable(
                name: "ObjetosCena");

            migrationBuilder.DropTable(
                name: "Modelos3d");

            migrationBuilder.DropIndex(
                name: "IX_TokensCena_ModeloId",
                table: "TokensCena");

            migrationBuilder.DropIndex(
                name: "IX_Personagens_ModeloId",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "ModeloId",
                table: "TokensCena");

            migrationBuilder.DropColumn(
                name: "ModeloId",
                table: "Personagens");
        }
    }
}
