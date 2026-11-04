using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VttVisaoDoToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Visao",
                table: "TokensCena",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visao",
                table: "TokensCena");
        }
    }
}
