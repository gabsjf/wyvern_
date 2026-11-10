using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PerfilDoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailPendente",
                table: "Usuario",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FotoVersao",
                table: "Usuario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenTrocaEmailExpiraEm",
                table: "Usuario",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenTrocaEmailHash",
                table: "Usuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dispositivo",
                table: "RefreshTokens",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UsuarioFoto",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioFoto", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_UsuarioFoto_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioFoto");

            migrationBuilder.DropColumn(
                name: "EmailPendente",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "FotoVersao",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TokenTrocaEmailExpiraEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TokenTrocaEmailHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "Dispositivo",
                table: "RefreshTokens");
        }
    }
}
