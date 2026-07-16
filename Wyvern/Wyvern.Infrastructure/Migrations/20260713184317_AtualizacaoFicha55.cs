using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AtualizacaoFicha55 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Antecedente",
                table: "PersonagemPlayers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subclasse",
                table: "PersonagemPlayers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tamanho",
                table: "PersonagemPlayers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClasseArmaduraEscudo",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DadoVidaGasto",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DadoVidaMaximo",
                table: "PersonagemCombates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathSaveFalhas",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeathSaveSucessos",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "InspiracaoHeroica",
                table: "PersonagemCombates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VidaTemporaria",
                table: "PersonagemCombates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaCarisma",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaConstituicao",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaDestreza",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaForca",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaInteligencia",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProficienciaSalvaguardaSabedoria",
                table: "Atributos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PersonagemConjuracoes",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    AtributoConjuracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModificadorConjuracao = table.Column<int>(type: "int", nullable: false),
                    CdMagia = table.Column<int>(type: "int", nullable: false),
                    ModificadorAtaqueMagico = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel1 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel1 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel2 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel2 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel3 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel3 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel4 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel4 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel5 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel5 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel6 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel6 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel7 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel7 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel8 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel8 = table.Column<int>(type: "int", nullable: false),
                    SlotsTotalNivel9 = table.Column<int>(type: "int", nullable: false),
                    SlotsGastosNivel9 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemConjuracoes", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagemConjuracoes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemDetalhes",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Aparencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoriaPersonalidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TracosEspecie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Talentos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaracteristicasClasse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProficienciaArmas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProficienciaFerramentas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Idiomas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProficienciaArmaduraLeve = table.Column<bool>(type: "bit", nullable: false),
                    ProficienciaArmaduraMedia = table.Column<bool>(type: "bit", nullable: false),
                    ProficienciaArmaduraPesada = table.Column<bool>(type: "bit", nullable: false),
                    ProficienciaEscudos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemDetalhes", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagemDetalhes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemDinheiros",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    PC = table.Column<int>(type: "int", nullable: false),
                    PP = table.Column<int>(type: "int", nullable: false),
                    PE = table.Column<int>(type: "int", nullable: false),
                    PO = table.Column<int>(type: "int", nullable: false),
                    PL = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemDinheiros", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagemDinheiros_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonagemConjuracoes");

            migrationBuilder.DropTable(
                name: "PersonagemDetalhes");

            migrationBuilder.DropTable(
                name: "PersonagemDinheiros");

            migrationBuilder.DropColumn(
                name: "Antecedente",
                table: "PersonagemPlayers");

            migrationBuilder.DropColumn(
                name: "Subclasse",
                table: "PersonagemPlayers");

            migrationBuilder.DropColumn(
                name: "Tamanho",
                table: "PersonagemPlayers");

            migrationBuilder.DropColumn(
                name: "ClasseArmaduraEscudo",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "DadoVidaGasto",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "DadoVidaMaximo",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "DeathSaveFalhas",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "DeathSaveSucessos",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "InspiracaoHeroica",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "VidaTemporaria",
                table: "PersonagemCombates");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaCarisma",
                table: "Atributos");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaConstituicao",
                table: "Atributos");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaDestreza",
                table: "Atributos");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaForca",
                table: "Atributos");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaInteligencia",
                table: "Atributos");

            migrationBuilder.DropColumn(
                name: "ProficienciaSalvaguardaSabedoria",
                table: "Atributos");
        }
    }
}
