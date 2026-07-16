using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonagemNpcEspeciais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonagemAcoesBonus",
                columns: table => new
                {
                    PersonagemAcaoBonusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAcoesBonus", x => x.PersonagemAcaoBonusId);
                    table.ForeignKey(
                        name: "FK_PersonagemAcoesBonus_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemAcoesLendarias",
                columns: table => new
                {
                    PersonagemAcaoLendariaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustoAcao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAcoesLendarias", x => x.PersonagemAcaoLendariaId);
                    table.ForeignKey(
                        name: "FK_PersonagemAcoesLendarias_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemAcoesPadrao",
                columns: table => new
                {
                    PersonagemAcaoPadraoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Alcance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BonusAcerto = table.Column<int>(type: "int", nullable: false),
                    Dano = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoDano = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Propriedades = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAcoesPadrao", x => x.PersonagemAcaoPadraoId);
                    table.ForeignKey(
                        name: "FK_PersonagemAcoesPadrao_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemReacoes",
                columns: table => new
                {
                    PersonagemReacaoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gatilho = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemReacoes", x => x.PersonagemReacaoId);
                    table.ForeignKey(
                        name: "FK_PersonagemReacoes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemTracosEspeciais",
                columns: table => new
                {
                    PersonagemTracoEspecialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemTracosEspeciais", x => x.PersonagemTracoEspecialId);
                    table.ForeignKey(
                        name: "FK_PersonagemTracosEspeciais_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagensNpc",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "int", nullable: false),
                    CategoriaUso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tamanho = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoCriatura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tendencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClasseArmaduraOrigem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormulaDadoVida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeslocamentoDescricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vulnerabilidades = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resistencias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImunidadesDano = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImunidadesCondicao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sentidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NivelDesafio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XpConcedido = table.Column<int>(type: "int", nullable: false),
                    VinculosIdeais = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SegredosFaccoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnotacoesLivres = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagensNpc", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagensNpc_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemAcoesBonus_PersonagemId",
                table: "PersonagemAcoesBonus",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemAcoesLendarias_PersonagemId",
                table: "PersonagemAcoesLendarias",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemAcoesPadrao_PersonagemId",
                table: "PersonagemAcoesPadrao",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemReacoes_PersonagemId",
                table: "PersonagemReacoes",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemTracosEspeciais_PersonagemId",
                table: "PersonagemTracosEspeciais",
                column: "PersonagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonagemAcoesBonus");

            migrationBuilder.DropTable(
                name: "PersonagemAcoesLendarias");

            migrationBuilder.DropTable(
                name: "PersonagemAcoesPadrao");

            migrationBuilder.DropTable(
                name: "PersonagemReacoes");

            migrationBuilder.DropTable(
                name: "PersonagemTracosEspeciais");

            migrationBuilder.DropTable(
                name: "PersonagensNpc");
        }
    }
}
