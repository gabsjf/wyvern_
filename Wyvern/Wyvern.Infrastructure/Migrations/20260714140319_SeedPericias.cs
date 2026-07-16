using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPericias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Pericias",
                columns: new[] { "PericiaId", "Ativo", "Nome" },
                values: new object[,]
                {
                    { 1, true, "Acrobacia" },
                    { 2, true, "Arcanismo" },
                    { 3, true, "Atletismo" },
                    { 4, true, "Atuação" },
                    { 5, true, "Enganação" },
                    { 6, true, "Furtividade" },
                    { 7, true, "História" },
                    { 8, true, "Intimidação" },
                    { 9, true, "Intuição" },
                    { 10, true, "Investigação" },
                    { 11, true, "Lidar com Animais" },
                    { 12, true, "Medicina" },
                    { 13, true, "Natureza" },
                    { 14, true, "Percepção" },
                    { 15, true, "Persuasão" },
                    { 16, true, "Prestidigitação" },
                    { 17, true, "Religião" },
                    { 18, true, "Sobrevivência" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Pericias",
                keyColumn: "PericiaId",
                keyValue: 18);
        }
    }
}
