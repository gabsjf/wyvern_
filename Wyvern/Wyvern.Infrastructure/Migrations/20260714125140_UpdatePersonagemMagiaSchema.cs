using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonagemMagiaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonagemMagias_Magias_MagiaId",
                table: "PersonagemMagias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonagemMagias",
                table: "PersonagemMagias");

            migrationBuilder.DropIndex(
                name: "IX_PersonagemMagias_MagiaId",
                table: "PersonagemMagias");

            migrationBuilder.RenameColumn(
                name: "MagiaId",
                table: "PersonagemMagias",
                newName: "Nivel");

            migrationBuilder.AddColumn<int>(
                name: "PersonagemMagiaId",
                table: "PersonagemMagias",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "PersonagemMagias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Escola",
                table: "PersonagemMagias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Material",
                table: "PersonagemMagias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "PersonagemMagias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Somatico",
                table: "PersonagemMagias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Verbal",
                table: "PersonagemMagias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonagemMagias",
                table: "PersonagemMagias",
                column: "PersonagemMagiaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemMagias_PersonagemId",
                table: "PersonagemMagias",
                column: "PersonagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonagemMagias",
                table: "PersonagemMagias");

            migrationBuilder.DropIndex(
                name: "IX_PersonagemMagias_PersonagemId",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "PersonagemMagiaId",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Escola",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Somatico",
                table: "PersonagemMagias");

            migrationBuilder.DropColumn(
                name: "Verbal",
                table: "PersonagemMagias");

            migrationBuilder.RenameColumn(
                name: "Nivel",
                table: "PersonagemMagias",
                newName: "MagiaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonagemMagias",
                table: "PersonagemMagias",
                columns: new[] { "PersonagemId", "MagiaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemMagias_MagiaId",
                table: "PersonagemMagias",
                column: "MagiaId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonagemMagias_Magias_MagiaId",
                table: "PersonagemMagias",
                column: "MagiaId",
                principalTable: "Magias",
                principalColumn: "MagiaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
