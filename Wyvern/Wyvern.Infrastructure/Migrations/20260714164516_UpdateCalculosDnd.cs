using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCalculosDnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClasseArmaduraOrigem",
                table: "PersonagensNpc");

            migrationBuilder.DropColumn(
                name: "DeslocamentoDescricao",
                table: "PersonagensNpc");

            migrationBuilder.DropColumn(
                name: "CdMagia",
                table: "PersonagemConjuracoes");

            migrationBuilder.DropColumn(
                name: "ModificadorAtaqueMagico",
                table: "PersonagemConjuracoes");

            migrationBuilder.DropColumn(
                name: "ModificadorConjuracao",
                table: "PersonagemConjuracoes");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel9",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel9Max");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel8",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel9Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel7",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel8Max");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel6",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel8Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel5",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel7Max");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel4",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel7Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel3",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel6Max");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel2",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel6Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsTotalNivel1",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel5Max");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel9",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel5Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel8",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel4Max");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel7",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel4Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel6",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel3Max");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel5",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel3Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel4",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel2Max");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel3",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel2Atual");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel2",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel1Max");

            migrationBuilder.RenameColumn(
                name: "SlotsGastosNivel1",
                table: "PersonagemConjuracoes",
                newName: "SlotsNivel1Atual");

            migrationBuilder.AlterColumn<string>(
                name: "Deslocamento",
                table: "PersonagemCombates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProficienciaBonus",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AtributoBase",
                table: "PersonagemAtaques",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Proficiente",
                table: "PersonagemAtaques",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AtributoBase",
                table: "PersonagemAcoesPadrao",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Proficiente",
                table: "PersonagemAcoesPadrao",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProficienciaBonus",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "AtributoBase",
                table: "PersonagemAtaques");

            migrationBuilder.DropColumn(
                name: "Proficiente",
                table: "PersonagemAtaques");

            migrationBuilder.DropColumn(
                name: "AtributoBase",
                table: "PersonagemAcoesPadrao");

            migrationBuilder.DropColumn(
                name: "Proficiente",
                table: "PersonagemAcoesPadrao");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel9Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel9");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel9Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel8");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel8Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel7");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel8Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel6");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel7Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel5");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel7Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel4");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel6Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel3");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel6Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel2");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel5Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsTotalNivel1");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel5Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel9");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel4Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel8");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel4Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel7");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel3Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel6");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel3Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel5");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel2Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel4");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel2Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel3");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel1Max",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel2");

            migrationBuilder.RenameColumn(
                name: "SlotsNivel1Atual",
                table: "PersonagemConjuracoes",
                newName: "SlotsGastosNivel1");

            migrationBuilder.AddColumn<string>(
                name: "ClasseArmaduraOrigem",
                table: "PersonagensNpc",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeslocamentoDescricao",
                table: "PersonagensNpc",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CdMagia",
                table: "PersonagemConjuracoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModificadorAtaqueMagico",
                table: "PersonagemConjuracoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModificadorConjuracao",
                table: "PersonagemConjuracoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Deslocamento",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
