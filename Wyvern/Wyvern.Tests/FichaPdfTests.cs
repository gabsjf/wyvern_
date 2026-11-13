using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Regras;

namespace Wyvern.Tests;

public class RegrasDndTests
{
    [Theory]
    [InlineData(1, -5)]
    [InlineData(7, -2)]
    [InlineData(8, -1)]
    [InlineData(9, -1)]   // a divisão inteira do C# daria 0
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(18, 4)]
    [InlineData(20, 5)]
    public void Modificador_arredonda_para_baixo(int pontuacao, int esperado)
    {
        Assert.Equal(esperado, RegrasDnd.Modificador(pontuacao));
    }

    [Theory]
    [InlineData(4, "+4")]
    [InlineData(0, "+0")]
    [InlineData(-1, "-1")]
    public void Bonus_ganha_sinal(int valor, string esperado)
    {
        Assert.Equal(esperado, RegrasDnd.ComSinal(valor));
    }
}

public class FichaPdfMapperTests
{
    // Kaelith Vorn, bardo 5: FOR 8, DES 14, CON 12, INT 11, SAB 10, CAR 18, bônus de proficiência +3
    private static Personagem Kaelith() => new()
    {
        Nome = "Kaelith Vorn",
        Atributo = new Atributo
        {
            Forca = 8, Destreza = 14, Constituicao = 12, Inteligencia = 11, Sabedoria = 10, Carisma = 18,
            ProficienciaSalvaguardaDestreza = true, ProficienciaSalvaguardaCarisma = true,
        },
        PersonagemPlayer = new PersonagemPlayer { Classe = "Bardo", Raca = "Meio-elfo", Nivel = 5, Alinhamento = "Neutro", Antecedente = "Artista", Xp = 6500 },
        PersonagemCombate = new PersonagemCombate { ClasseArmadura = 15, VidaMaxima = 38, VidaAtual = 31, Deslocamento = "9m", ProficienciaBonus = 3, DeathSaveSucessos = 2, DeathSaveFalhas = 1 },
        PersonagemPericias = new List<PersonagemPericia>
        {
            // Basta a perícia constar na lista: o cadastro não preenche o campo Proficiente.
            new() { Pericia = new Pericia { Nome = "Atuação" } },
            new() { Bonus = 2, Pericia = new Pericia { Nome = "Persuasão" } },
        },
        PersonagemAtaques = new List<PersonagemAtaque>
        {
            new() { Nome = "Rapieira", AtributoBase = "Destreza", Proficiente = true, Dano = "1d8", TipoDano = "perfurante" },
        },
        PersonagemConjuracao = new PersonagemConjuracao { AtributoConjuracao = "Carisma", SlotsNivel1Max = 4, SlotsNivel1Atual = 3 },
        PersonagemMagias = new List<PersonagemMagia>
        {
            new() { Nome = "Zombaria Viciosa", Nivel = 0 },
            new() { Nome = "Curar Ferimentos", Nivel = 1 },
        },
    };

    [Fact]
    public void Identificacao_atributos_e_combate()
    {
        var t = FichaPdfMapper.Mapear(Kaelith()).Textos;

        Assert.Equal("Kaelith Vorn", t["CharacterName"]);
        Assert.Equal("Bardo 5", t["ClassLevel"]);
        Assert.Equal("Meio-elfo", t["Race"]);
        Assert.Equal("-1", t["STR"]);      // quadrado grande: modificador
        Assert.Equal("8", t["STRmod"]);    // oval: pontuação
        Assert.Equal("18", t["CHamod"]);
        Assert.Equal("+4", t["CHA"]);
        Assert.Equal("15", t["AC"]);
        Assert.Equal("+3", t["ProfBonus"]);
        Assert.Equal("+2", t["Initiative"]);   // sem iniciativa cadastrada: usa o modificador de Destreza
        Assert.Equal("38", t["HPMax"]);
        Assert.Equal("31", t["HPCurrent"]);
    }

    [Fact]
    public void Salvaguardas_e_pericias_somam_proficiencia_e_bonus()
    {
        var ficha = FichaPdfMapper.Mapear(Kaelith());

        Assert.Equal("-1", ficha.Textos["ST Strength"]);
        Assert.False(ficha.Caixas["Check Box 11"]);
        Assert.Equal("+7", ficha.Textos["ST Charisma"]);   // +4 de Carisma, +3 de proficiência
        Assert.True(ficha.Caixas["Check Box 22"]);

        Assert.Equal("+7", ficha.Textos["Performance"]);   // Atuação: CAR +4, proficiente +3
        Assert.True(ficha.Caixas["Check Box 35"]);
        Assert.Equal("+9", ficha.Textos["Persuasion"]);    // +4, proficiente +3, bônus extra +2
        Assert.Equal("+4", ficha.Textos["Deception"]);     // Enganação sem proficiência
        Assert.False(ficha.Caixas["Check Box 27"]);
        Assert.Equal("10", ficha.Textos["Passive"]);       // 10 + Percepção (+0)
    }

    [Fact]
    public void Salvaguardas_de_morte_marcam_as_caixas_certas()
    {
        var caixas = FichaPdfMapper.Mapear(Kaelith()).Caixas;

        Assert.True(caixas["Check Box 12"]);
        Assert.True(caixas["Check Box 13"]);
        Assert.False(caixas["Check Box 14"]);
        Assert.True(caixas["Check Box 15"]);
        Assert.False(caixas["Check Box 16"]);
    }

    [Fact]
    public void Ataque_usa_a_mesma_conta_da_ficha_na_tela()
    {
        var t = FichaPdfMapper.Mapear(Kaelith()).Textos;

        Assert.Equal("Rapieira", t["Wpn Name"]);
        Assert.Equal("+5", t["Wpn1 AtkBonus"]);              // DES +2, proficiente +3
        Assert.Equal("1d8+2 perfurante", t["Wpn1 Damage"]);
    }

    [Fact]
    public void Conjuracao_preenche_atributo_CD_bonus_e_espacos()
    {
        var t = FichaPdfMapper.Mapear(Kaelith()).Textos;

        Assert.Equal("Carisma", t["SpellcastingAbility 2"]);
        Assert.Equal("15", t["SpellSaveDC 2"]);              // 8 + 3 + 4
        Assert.Equal("+7", t["SpellAtkBonus 2"]);
        Assert.Equal("4", t["SlotsTotal 19"]);
        Assert.Equal("1", t["SlotsRemaining 19"]);           // 4 no total, 3 restantes: 1 gasto
        Assert.False(t.ContainsKey("SlotsTotal 20"));        // círculo 2 sem espaços: fica em branco
    }

    [Fact]
    public void Magias_vao_para_a_linha_do_nivel_certo()
    {
        var t = FichaPdfMapper.Mapear(Kaelith()).Textos;

        Assert.Equal("Zombaria Viciosa", t["Spells 1014"]);  // truque: primeira linha
        Assert.Equal("Curar Ferimentos", t["Spells 1015"]);  // 1º círculo: primeira linha
    }

    [Fact]
    public void Magias_alem_das_linhas_disponiveis_nao_somem_sem_aviso()
    {
        var p = Kaelith();
        p.PersonagemMagias = Enumerable.Range(1, 9).Select(i => new PersonagemMagia { Nivel = 0, Nome = $"Truque {i}" }).ToList();

        var t = FichaPdfMapper.Mapear(p).Textos;

        Assert.Equal("Truque 1", t["Spells 1014"]);
        Assert.Contains("(+1 magias)", t["Spells 1022"]);    // a ficha só tem 8 linhas de truques
    }

    [Theory]
    [InlineData("Atuação", "atuacao")]
    [InlineData("  Lidar com Animais ", "lidar com animais")]
    [InlineData("Prestidigitação", "prestidigitacao")]
    public void Normalizar_remove_acentos_e_caixa(string entrada, string esperado)
    {
        Assert.Equal(esperado, FichaPdfMapper.Normalizar(entrada));
    }
}

public class PdfExportPontaAPontaTests
{
    private static string Modelo => Path.Combine(AppContext.BaseDirectory, "Templates", "ficha55.pdf");

    // Lê de volta o PDF gerado: percorre as anotações e devolve /V de cada campo, pelo nome.
    private static Dictionary<string, string> LerCampos(byte[] pdf)
    {
        using var ms = new MemoryStream(pdf);
        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        var campos = new Dictionary<string, string>();

        for (var i = 0; i < doc.PageCount; i++)
        {
            var anotacoes = doc.Pages[i].Elements.GetArray("/Annots");
            if (anotacoes == null) continue;

            foreach (var item in anotacoes.Elements)
            {
                var d = (item as PdfReference)?.Value as PdfDictionary;
                var nome = d?.Elements.GetString("/T");
                if (d == null || string.IsNullOrEmpty(nome)) continue;

                var v = d.Elements["/V"];
                campos[string.Join(' ', nome.Split(' ', StringSplitOptions.RemoveEmptyEntries))] = v switch
                {
                    PdfString s => s.Value,
                    PdfName n => n.Value,
                    _ => "",
                };
            }
        }

        return campos;
    }

    [Fact]
    public void Exporta_a_ficha_preenchida_com_acentos_e_caixas_marcadas()
    {
        var p = new Personagem
        {
            Nome = "Kaelith Vorn",
            Atributo = new Atributo { Forca = 8, Destreza = 14, Constituicao = 12, Inteligencia = 11, Sabedoria = 10, Carisma = 18, ProficienciaSalvaguardaCarisma = true },
            PersonagemPlayer = new PersonagemPlayer { Classe = "Bardo", Raca = "Meio-elfo", Nivel = 5, Alinhamento = "Neutro" },
            PersonagemCombate = new PersonagemCombate { ClasseArmadura = 15, VidaMaxima = 38, VidaAtual = 31, ProficienciaBonus = 3 },
            PersonagemMagias = new List<PersonagemMagia> { new() { Nome = "Zombaria Viciosa", Nivel = 0 } },
            PersonagemItens = new List<PersonagemItem> { new() { Nome = "Alaúde" } },
        };

        var bytes = new PdfExportService().ExportPdf(p, Modelo);
        var campos = LerCampos(bytes);

        Assert.Equal("Kaelith Vorn", campos["CharacterName"]);
        Assert.Equal("Bardo 5", campos["ClassLevel"]);
        Assert.Equal("Meio-elfo", campos["Race"]);        // o campo do modelo se chama "Race " (com espaço)
        Assert.Equal("8", campos["STRmod"]);       // pontuação no oval
        Assert.Equal("-1", campos["STR"]);         // modificador no quadrado grande
        Assert.Equal("38", campos["HPMax"]);
        Assert.Equal("Zombaria Viciosa", campos["Spells 1014"]);
        Assert.Contains("Alaúde", campos["Equipment"]);   // acento preservado
        Assert.Equal("/Yes", campos["Check Box 22"]);     // salvaguarda de Carisma proficiente: caixa marcada
        Assert.Equal("/Off", campos["Check Box 11"]);     // Força sem proficiência
    }

    [Fact]
    public void Personagem_sem_dados_extras_exporta_sem_falhar()
    {
        var bytes = new PdfExportService().ExportPdf(new Personagem { Nome = "Vazio" }, Modelo);

        Assert.Equal("Vazio", LerCampos(bytes)["CharacterName"]);
    }
}
