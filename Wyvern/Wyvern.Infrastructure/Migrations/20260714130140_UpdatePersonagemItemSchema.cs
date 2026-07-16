using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonagemItemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonagemItens_Itens_ItemId",
                table: "PersonagemItens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonagemItens",
                table: "PersonagemItens");

            migrationBuilder.DropIndex(
                name: "IX_PersonagemItens_ItemId",
                table: "PersonagemItens");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "PersonagemItens");

            migrationBuilder.AddColumn<int>(
                name: "PersonagemItemId",
                table: "PersonagemItens",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "PersonagemItens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Raridade",
                table: "PersonagemItens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoItem",
                table: "PersonagemItens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonagemItens",
                table: "PersonagemItens",
                column: "PersonagemItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemItens_PersonagemId",
                table: "PersonagemItens",
                column: "PersonagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonagemItens",
                table: "PersonagemItens");

            migrationBuilder.DropIndex(
                name: "IX_PersonagemItens_PersonagemId",
                table: "PersonagemItens");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "PersonagemItens");

            migrationBuilder.DropColumn(
                name: "Raridade",
                table: "PersonagemItens");

            migrationBuilder.DropColumn(
                name: "TipoItem",
                table: "PersonagemItens");

            migrationBuilder.RenameColumn(
                name: "PersonagemItemId",
                table: "PersonagemItens",
                newName: "ItemId");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "PersonagemItens",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonagemItens",
                table: "PersonagemItens",
                columns: new[] { "PersonagemId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemItens_ItemId",
                table: "PersonagemItens",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonagemItens_Itens_ItemId",
                table: "PersonagemItens",
                column: "ItemId",
                principalTable: "Itens",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
