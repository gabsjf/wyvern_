using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanosDeConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plano",
                table: "Usuario",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanoAlteradoEm",
                table: "Usuario",
                type: "timestamp without time zone",
                nullable: true);

            // Antes qualquer conta criava campanha. Quem já é dono de alguma vira Mestre, para não perder o que criou
            // quando os limites passarem a valer (fim do beta).
            migrationBuilder.Sql("""
                UPDATE "Usuario" SET "Papel" = 'Mestre'
                WHERE "Papel" <> 'Mestre'
                  AND EXISTS (SELECT 1 FROM "Campanhas" c WHERE c."MestreId" = "Usuario"."UsuarioId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plano",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PlanoAlteradoEm",
                table: "Usuario");
        }
    }
}
