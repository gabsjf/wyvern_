using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmacaoDeEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmadoEm",
                table: "Usuario",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenConfirmacaoExpiraEm",
                table: "Usuario",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenConfirmacaoHash",
                table: "Usuario",
                type: "text",
                nullable: true);

            // Quem já tinha conta não fica trancado para fora: conta existente conta como confirmada.
            migrationBuilder.Sql("UPDATE \"Usuario\" SET \"EmailConfirmadoEm\" = \"CriadoEm\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmadoEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TokenConfirmacaoExpiraEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TokenConfirmacaoHash",
                table: "Usuario");
        }
    }
}
