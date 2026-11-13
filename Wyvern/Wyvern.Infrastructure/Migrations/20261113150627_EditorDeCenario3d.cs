using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditorDeCenario3d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Rotacao",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<double>(
                name: "EscalaX",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EscalaY",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EscalaZ",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GiroX",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GiroZ",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "X",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Y",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Z",
                table: "ObjetosCena",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            // as peças que já estavam nos mapas continuam onde estavam: centro da área e escala igual ao lado dela
            migrationBuilder.Sql(
                "UPDATE \"ObjetosCena\" SET \"X\" = \"Coluna\" + \"Tamanho\" / 2.0, \"Z\" = \"Linha\" + \"Tamanho\" / 2.0, " +
                "\"EscalaX\" = \"Tamanho\", \"EscalaY\" = \"Tamanho\", \"EscalaZ\" = \"Tamanho\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalaX",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "EscalaY",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "EscalaZ",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "GiroX",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "GiroZ",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "X",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "ObjetosCena");

            migrationBuilder.DropColumn(
                name: "Z",
                table: "ObjetosCena");

            migrationBuilder.AlterColumn<int>(
                name: "Rotacao",
                table: "ObjetosCena",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }
    }
}
