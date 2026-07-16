using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wyvern.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Itens",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itens", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "Magias",
                columns: table => new
                {
                    MagiaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Magias", x => x.MagiaId);
                });

            migrationBuilder.CreateTable(
                name: "Pericias",
                columns: table => new
                {
                    PericiaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pericias", x => x.PericiaId);
                });

            migrationBuilder.CreateTable(
                name: "TipoPersonagem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoPersonagem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "Campanhas",
                columns: table => new
                {
                    CampanhaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Sistema = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MestreId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas", x => x.CampanhaId);
                    table.ForeignKey(
                        name: "FK_Campanhas_Usuario_MestreId",
                        column: x => x.MestreId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PastasAnotacao",
                columns: table => new
                {
                    PastaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsPublica = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastasAnotacao", x => x.PastaId);
                    table.ForeignKey(
                        name: "FK_PastasAnotacao_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personagens",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    TipoId = table.Column<int>(type: "integer", nullable: false),
                    CriadoPorId = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personagens", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_Personagens_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Personagens_TipoPersonagem_TipoId",
                        column: x => x.TipoId,
                        principalTable: "TipoPersonagem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Personagens_Usuario_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                columns: table => new
                {
                    SessaoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroSessao = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    DataSessao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAgendada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Obs = table.Column<string>(type: "text", nullable: false),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.SessaoId);
                    table.ForeignKey(
                        name: "FK_Sessoes_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Anotacoes",
                columns: table => new
                {
                    AnotacaoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    PastaId = table.Column<int>(type: "integer", nullable: true),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    IsPublica = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anotacoes", x => x.AnotacaoId);
                    table.ForeignKey(
                        name: "FK_Anotacoes_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "CampanhaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Anotacoes_PastasAnotacao_PastaId",
                        column: x => x.PastaId,
                        principalTable: "PastasAnotacao",
                        principalColumn: "PastaId");
                });

            migrationBuilder.CreateTable(
                name: "Atributos",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Forca = table.Column<int>(type: "integer", nullable: false),
                    Destreza = table.Column<int>(type: "integer", nullable: false),
                    Constituicao = table.Column<int>(type: "integer", nullable: false),
                    Inteligencia = table.Column<int>(type: "integer", nullable: false),
                    Sabedoria = table.Column<int>(type: "integer", nullable: false),
                    Carisma = table.Column<int>(type: "integer", nullable: false),
                    ProficienciaSalvaguardaForca = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaSalvaguardaDestreza = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaSalvaguardaConstituicao = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaSalvaguardaInteligencia = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaSalvaguardaSabedoria = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaSalvaguardaCarisma = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atributos", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_Atributos_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemAcoesBonus",
                columns: table => new
                {
                    PersonagemAcaoBonusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
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
                    PersonagemAcaoLendariaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CustoAcao = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: false)
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
                    PersonagemAcaoPadraoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Alcance = table.Column<string>(type: "text", nullable: true),
                    BonusAcerto = table.Column<int>(type: "integer", nullable: false),
                    Dano = table.Column<string>(type: "text", nullable: true),
                    TipoDano = table.Column<string>(type: "text", nullable: true),
                    Propriedades = table.Column<string>(type: "text", nullable: true),
                    AtributoBase = table.Column<string>(type: "text", nullable: true),
                    Proficiente = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "PersonagemAtaques",
                columns: table => new
                {
                    PersonagemAtaqueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Alcance = table.Column<string>(type: "text", nullable: false),
                    BonusAcerto = table.Column<int>(type: "integer", nullable: false),
                    Dano = table.Column<string>(type: "text", nullable: false),
                    TipoDano = table.Column<string>(type: "text", nullable: false),
                    Propriedades = table.Column<string>(type: "text", nullable: false),
                    AtributoBase = table.Column<string>(type: "text", nullable: true),
                    Proficiente = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAtaques", x => x.PersonagemAtaqueId);
                    table.ForeignKey(
                        name: "FK_PersonagemAtaques_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemCombates",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    VidaAtual = table.Column<int>(type: "integer", nullable: false),
                    VidaMaxima = table.Column<int>(type: "integer", nullable: false),
                    ClasseArmadura = table.Column<int>(type: "integer", nullable: false),
                    Iniciativa = table.Column<int>(type: "integer", nullable: false),
                    Deslocamento = table.Column<string>(type: "text", nullable: true),
                    ProficienciaBonus = table.Column<int>(type: "integer", nullable: false),
                    InspiracaoHeroica = table.Column<bool>(type: "boolean", nullable: false),
                    VidaTemporaria = table.Column<int>(type: "integer", nullable: false),
                    DadoVidaMaximo = table.Column<string>(type: "text", nullable: true),
                    DadoVidaGasto = table.Column<int>(type: "integer", nullable: false),
                    DeathSaveSucessos = table.Column<int>(type: "integer", nullable: false),
                    DeathSaveFalhas = table.Column<int>(type: "integer", nullable: false),
                    ClasseArmaduraEscudo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemCombates", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagemCombates_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemConjuracoes",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    AtributoConjuracao = table.Column<string>(type: "text", nullable: true),
                    SlotsNivel1Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel2Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel3Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel4Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel5Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel6Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel7Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel8Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel9Max = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel1Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel2Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel3Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel4Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel5Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel6Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel7Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel8Atual = table.Column<int>(type: "integer", nullable: false),
                    SlotsNivel9Atual = table.Column<int>(type: "integer", nullable: false)
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
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Aparencia = table.Column<string>(type: "text", nullable: true),
                    HistoriaPersonalidade = table.Column<string>(type: "text", nullable: true),
                    TracosEspecie = table.Column<string>(type: "text", nullable: true),
                    Talentos = table.Column<string>(type: "text", nullable: true),
                    CaracteristicasClasse = table.Column<string>(type: "text", nullable: true),
                    ProficienciaArmas = table.Column<string>(type: "text", nullable: true),
                    ProficienciaFerramentas = table.Column<string>(type: "text", nullable: true),
                    Idiomas = table.Column<string>(type: "text", nullable: true),
                    ProficienciaArmaduraLeve = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaArmaduraMedia = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaArmaduraPesada = table.Column<bool>(type: "boolean", nullable: false),
                    ProficienciaEscudos = table.Column<bool>(type: "boolean", nullable: false)
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
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    PC = table.Column<int>(type: "integer", nullable: false),
                    PP = table.Column<int>(type: "integer", nullable: false),
                    PE = table.Column<int>(type: "integer", nullable: false),
                    PO = table.Column<int>(type: "integer", nullable: false),
                    PL = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PersonagemItens",
                columns: table => new
                {
                    PersonagemItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    TipoItem = table.Column<string>(type: "text", nullable: false),
                    Raridade = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemItens", x => x.PersonagemItemId);
                    table.ForeignKey(
                        name: "FK_PersonagemItens_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemMagias",
                columns: table => new
                {
                    PersonagemMagiaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Escola = table.Column<string>(type: "text", nullable: false),
                    Verbal = table.Column<bool>(type: "boolean", nullable: false),
                    Somatico = table.Column<bool>(type: "boolean", nullable: false),
                    Material = table.Column<bool>(type: "boolean", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemMagias", x => x.PersonagemMagiaId);
                    table.ForeignKey(
                        name: "FK_PersonagemMagias_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemPericias",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    PericiaId = table.Column<int>(type: "integer", nullable: false),
                    Bonus = table.Column<int>(type: "integer", nullable: false),
                    Proficiente = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemPericias", x => new { x.PersonagemId, x.PericiaId });
                    table.ForeignKey(
                        name: "FK_PersonagemPericias_Pericias_PericiaId",
                        column: x => x.PericiaId,
                        principalTable: "Pericias",
                        principalColumn: "PericiaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonagemPericias_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemPlayers",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Classe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Raca = table.Column<string>(type: "text", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false),
                    Alinhamento = table.Column<string>(type: "text", nullable: false),
                    Antecedente = table.Column<string>(type: "text", nullable: true),
                    Subclasse = table.Column<string>(type: "text", nullable: true),
                    Tamanho = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemPlayers", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_PersonagemPlayers_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonagemReacoes",
                columns: table => new
                {
                    PersonagemReacaoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Gatilho = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: false)
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
                    PersonagemTracoEspecialId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
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
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    CategoriaUso = table.Column<string>(type: "text", nullable: true),
                    Tamanho = table.Column<string>(type: "text", nullable: true),
                    TipoCriatura = table.Column<string>(type: "text", nullable: true),
                    Tendencia = table.Column<string>(type: "text", nullable: true),
                    FormulaDadoVida = table.Column<string>(type: "text", nullable: true),
                    Vulnerabilidades = table.Column<string>(type: "text", nullable: true),
                    Resistencias = table.Column<string>(type: "text", nullable: true),
                    ImunidadesDano = table.Column<string>(type: "text", nullable: true),
                    ImunidadesCondicao = table.Column<string>(type: "text", nullable: true),
                    Sentidos = table.Column<string>(type: "text", nullable: true),
                    NivelDesafio = table.Column<string>(type: "text", nullable: true),
                    XpConcedido = table.Column<int>(type: "integer", nullable: false),
                    VinculosIdeais = table.Column<string>(type: "text", nullable: true),
                    SegredosFaccoes = table.Column<string>(type: "text", nullable: true),
                    AnotacoesLivres = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Combates",
                columns: table => new
                {
                    CombateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessaoId = table.Column<int>(type: "integer", nullable: false),
                    RodadaAtual = table.Column<int>(type: "integer", nullable: false),
                    TurnoAtualIndex = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combates", x => x.CombateId);
                    table.ForeignKey(
                        name: "FK_Combates_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "SessaoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombateParticipantes",
                columns: table => new
                {
                    ParticipanteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CombateId = table.Column<int>(type: "integer", nullable: false),
                    PersonagemId = table.Column<int>(type: "integer", nullable: true),
                    NomeNPC = table.Column<string>(type: "text", nullable: true),
                    Iniciativa = table.Column<int>(type: "integer", nullable: false),
                    VidaAtual = table.Column<int>(type: "integer", nullable: false),
                    VidaMaxima = table.Column<int>(type: "integer", nullable: false),
                    ClasseArmadura = table.Column<int>(type: "integer", nullable: false),
                    IsInimigo = table.Column<bool>(type: "boolean", nullable: false),
                    Condicoes = table.Column<string>(type: "text", nullable: false),
                    SucessosMorte = table.Column<int>(type: "integer", nullable: false),
                    FalhasMorte = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombateParticipantes", x => x.ParticipanteId);
                    table.ForeignKey(
                        name: "FK_CombateParticipantes_Combates_CombateId",
                        column: x => x.CombateId,
                        principalTable: "Combates",
                        principalColumn: "CombateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombateParticipantes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "PersonagemId");
                });

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

            migrationBuilder.InsertData(
                table: "TipoPersonagem",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Jogador" },
                    { 2, "NPC" },
                    { 3, "Monstro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anotacoes_CampanhaId",
                table: "Anotacoes",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anotacoes_PastaId",
                table: "Anotacoes",
                column: "PastaId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_MestreId",
                table: "Campanhas",
                column: "MestreId");

            migrationBuilder.CreateIndex(
                name: "IX_CombateParticipantes_CombateId",
                table: "CombateParticipantes",
                column: "CombateId");

            migrationBuilder.CreateIndex(
                name: "IX_CombateParticipantes_PersonagemId",
                table: "CombateParticipantes",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_SessaoId",
                table: "Combates",
                column: "SessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PastasAnotacao_CampanhaId",
                table: "PastasAnotacao",
                column: "CampanhaId");

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
                name: "IX_PersonagemAtaques_PersonagemId",
                table: "PersonagemAtaques",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemItens_PersonagemId",
                table: "PersonagemItens",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemMagias_PersonagemId",
                table: "PersonagemMagias",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemPericias_PericiaId",
                table: "PersonagemPericias",
                column: "PericiaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemReacoes_PersonagemId",
                table: "PersonagemReacoes",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemTracosEspeciais_PersonagemId",
                table: "PersonagemTracosEspeciais",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_CampanhaId",
                table: "Personagens",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_CriadoPorId",
                table: "Personagens",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_TipoId",
                table: "Personagens",
                column: "TipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_CampanhaId",
                table: "Sessoes",
                column: "CampanhaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anotacoes");

            migrationBuilder.DropTable(
                name: "Atributos");

            migrationBuilder.DropTable(
                name: "CombateParticipantes");

            migrationBuilder.DropTable(
                name: "Itens");

            migrationBuilder.DropTable(
                name: "Magias");

            migrationBuilder.DropTable(
                name: "PersonagemAcoesBonus");

            migrationBuilder.DropTable(
                name: "PersonagemAcoesLendarias");

            migrationBuilder.DropTable(
                name: "PersonagemAcoesPadrao");

            migrationBuilder.DropTable(
                name: "PersonagemAtaques");

            migrationBuilder.DropTable(
                name: "PersonagemCombates");

            migrationBuilder.DropTable(
                name: "PersonagemConjuracoes");

            migrationBuilder.DropTable(
                name: "PersonagemDetalhes");

            migrationBuilder.DropTable(
                name: "PersonagemDinheiros");

            migrationBuilder.DropTable(
                name: "PersonagemItens");

            migrationBuilder.DropTable(
                name: "PersonagemMagias");

            migrationBuilder.DropTable(
                name: "PersonagemPericias");

            migrationBuilder.DropTable(
                name: "PersonagemPlayers");

            migrationBuilder.DropTable(
                name: "PersonagemReacoes");

            migrationBuilder.DropTable(
                name: "PersonagemTracosEspeciais");

            migrationBuilder.DropTable(
                name: "PersonagensNpc");

            migrationBuilder.DropTable(
                name: "PastasAnotacao");

            migrationBuilder.DropTable(
                name: "Combates");

            migrationBuilder.DropTable(
                name: "Pericias");

            migrationBuilder.DropTable(
                name: "Personagens");

            migrationBuilder.DropTable(
                name: "Sessoes");

            migrationBuilder.DropTable(
                name: "TipoPersonagem");

            migrationBuilder.DropTable(
                name: "Campanhas");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
