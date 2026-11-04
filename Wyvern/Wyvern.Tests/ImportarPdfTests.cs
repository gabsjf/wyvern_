using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

public class FichaPdfLeitorTests
{
    [Theory]
    [InlineData("Bardo 5", "Bardo", null, 5)]
    [InlineData("Bardo (Colégio da Bravura) 5", "Bardo", "Colégio da Bravura", 5)]
    [InlineData("Fighter 12", "Fighter", null, 12)]
    [InlineData("Guerreiro 3 / Mago 2", "Guerreiro 3 / Mago 2", null, 5)]   // multiclasse: soma os níveis
    [InlineData("Ladino", "Ladino", null, 1)]
    [InlineData("", "", null, 1)]
    [InlineData("Mago 99", "Mago", null, 20)]
    public void Classe_e_nivel_sao_separados(string texto, string classe, string? subclasse, int nivel)
    {
        Assert.Equal((classe, subclasse, nivel), FichaPdfLeitor.LerClasse(texto));
    }

    [Theory]
    [InlineData("+3", 3)]
    [InlineData("-1", -1)]
    [InlineData(" 12 ", 12)]
    [InlineData("9m", 9)]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    public void Numeros_toleram_sinal_e_lixo(string texto, int esperado)
    {
        Assert.Equal(esperado, FichaPdfLeitor.Inteiro(texto));
    }

    [Fact]
    public void Ficha_em_branco_ou_incompleta_nao_quebra()
    {
        var p = FichaPdfLeitor.Ler(new Dictionary<string, string>(), new Dictionary<string, bool>(), campanhaId: 1, criadoPorId: 1);

        Assert.Equal("Personagem Desconhecido", p.Nome);
        Assert.Equal(10, p.Atributo.Forca);      // atributo vazio vira 10, não 0 (modificador -5)
        Assert.Equal(1, p.PersonagemPlayer.Nivel);
        Assert.Empty(p.PersonagemPericias);
        Assert.Empty(p.PersonagemMagias);
    }

    [Fact]
    public void Ficha_de_outra_origem_em_ingles_e_lida()
    {
        // Uma ficha preenchida à mão em inglês: sem os textos "Traços de espécie:" etc.
        var textos = new Dictionary<string, string>
        {
            ["CharacterName"] = "Thorin", ["ClassLevel"] = "Fighter 3", ["Race"] = "Dwarf", ["STR"] = "16", ["CON"] = "14",
            ["HPMax"] = "28", ["AC"] = "18", ["Equipment"] = "Longsword\nShield\n10 torches", ["Spells 1014"] = "Light (+2 magias)",
        };
        var caixas = new Dictionary<string, bool> { ["Check Box 11"] = true, ["Check Box 26"] = true };

        var p = FichaPdfLeitor.Ler(textos, caixas, 1, 1);

        Assert.Equal("Fighter", p.PersonagemPlayer.Classe);
        Assert.Equal(3, p.PersonagemPlayer.Nivel);
        Assert.Equal(16, p.Atributo.Forca);
        Assert.True(p.Atributo.ProficienciaSalvaguardaForca);
        Assert.Equal(28, p.PersonagemCombate.VidaAtual);   // sem HP atual na ficha: assume cheio
        Assert.Equal(3, p.PersonagemItens.Count);
        Assert.Contains(p.PersonagemPericias, x => x.Pericia.Nome.Contains("Atletismo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Light", p.PersonagemMagias.Single().Nome);
    }
}

public class ImportarPdfPontaAPontaTests
{
    private static string Modelo => Path.Combine(AppContext.BaseDirectory, "Templates", "ficha55.pdf");

    private static Personagem Kaelith() => new()
    {
        Nome = "Kaelith Vorn",
        Atributo = new Atributo
        {
            Forca = 8, Destreza = 14, Constituicao = 12, Inteligencia = 11, Sabedoria = 10, Carisma = 18,
            ProficienciaSalvaguardaDestreza = true, ProficienciaSalvaguardaCarisma = true,
        },
        PersonagemPlayer = new PersonagemPlayer { Classe = "Bardo", Subclasse = "Colégio da Bravura", Raca = "Meio-elfo", Nivel = 5, Alinhamento = "Neutro", Antecedente = "Artista", Xp = 6500 },
        PersonagemCombate = new PersonagemCombate { ClasseArmadura = 15, VidaMaxima = 38, VidaAtual = 31, VidaTemporaria = 4, Deslocamento = "9m", ProficienciaBonus = 3, DeathSaveSucessos = 2, DeathSaveFalhas = 1, DadoVidaMaximo = "5d8" },
        PersonagemPericias = new List<PersonagemPericia>
        {
            new() { Pericia = new Pericia { Nome = "Atuação" } },
            new() { Bonus = 2, Pericia = new Pericia { Nome = "Persuasão" } },
        },
        PersonagemAtaques = new List<PersonagemAtaque>
        {
            new() { Nome = "Rapieira", AtributoBase = "Destreza", Proficiente = true, Dano = "1d8", TipoDano = "perfurante" },
        },
        PersonagemDinheiro = new PersonagemDinheiro { PC = 1, PP = 2, PE = 3, PO = 150, PL = 4 },
        PersonagemItens = new List<PersonagemItem> { new() { Nome = "Alaúde", Raridade = "Comum" }, new() { Nome = "Adaga" } },
        PersonagemDetalhes = new PersonagemDetalhes
        {
            CaracteristicasClasse = "Inspiração de Bardo (d8)",
            TracosEspecie = "Visão no Escuro",
            Talentos = "Artista Nato",
            Idiomas = "Comum, Élfico",
            ProficienciaArmaduraLeve = true,
            ProficienciaArmas = "Armas simples, rapieiras",
            HistoriaPersonalidade = "Cresceu em uma trupe de teatro.",
        },
        PersonagemConjuracao = new PersonagemConjuracao { AtributoConjuracao = "Carisma", SlotsNivel1Max = 4, SlotsNivel1Atual = 3, SlotsNivel2Max = 3, SlotsNivel2Atual = 3 },
        PersonagemMagias = new List<PersonagemMagia>
        {
            new() { Nome = "Zombaria Viciosa", Nivel = 0 },
            new() { Nome = "Curar Ferimentos", Nivel = 1 },
            new() { Nome = "Sugestão", Nivel = 2 },
        },
    };

    private static Personagem ExportarEImportar(Personagem original)
    {
        var bytes = new PdfExportService().ExportPdf(original, Modelo);
        using var ms = new MemoryStream(bytes);
        return new PdfParserService().ParsePdf(ms, campanhaId: 7, criadoPorId: 9);
    }

    [Fact]
    public void Exportar_e_importar_devolve_a_mesma_ficha()
    {
        var p = ExportarEImportar(Kaelith());

        Assert.Equal("Kaelith Vorn", p.Nome);
        Assert.Equal(7, p.CampanhaId);
        Assert.Equal(9, p.CriadoPorId);

        Assert.Equal("Bardo", p.PersonagemPlayer.Classe);
        Assert.Equal("Colégio da Bravura", p.PersonagemPlayer.Subclasse);
        Assert.Equal(5, p.PersonagemPlayer.Nivel);
        Assert.Equal("Meio-elfo", p.PersonagemPlayer.Raca);
        Assert.Equal("Artista", p.PersonagemPlayer.Antecedente);
        Assert.Equal(6500, p.PersonagemPlayer.Xp);

        Assert.Equal((8, 14, 12, 11, 10, 18), (p.Atributo.Forca, p.Atributo.Destreza, p.Atributo.Constituicao, p.Atributo.Inteligencia, p.Atributo.Sabedoria, p.Atributo.Carisma));
        Assert.True(p.Atributo.ProficienciaSalvaguardaDestreza);
        Assert.True(p.Atributo.ProficienciaSalvaguardaCarisma);
        Assert.False(p.Atributo.ProficienciaSalvaguardaForca);
    }

    [Fact]
    public void Combate_pericias_e_ataques_voltam_certos()
    {
        var p = ExportarEImportar(Kaelith());

        Assert.Equal(15, p.PersonagemCombate.ClasseArmadura);
        Assert.Equal(38, p.PersonagemCombate.VidaMaxima);
        Assert.Equal(31, p.PersonagemCombate.VidaAtual);
        Assert.Equal(4, p.PersonagemCombate.VidaTemporaria);
        Assert.Equal(0, p.PersonagemCombate.Iniciativa);          // não cadastrada: continua não cadastrada
        Assert.Equal("9m", p.PersonagemCombate.Deslocamento);
        Assert.Equal(3, p.PersonagemCombate.ProficienciaBonus);
        Assert.Equal(2, p.PersonagemCombate.DeathSaveSucessos);
        Assert.Equal(1, p.PersonagemCombate.DeathSaveFalhas);
        Assert.Equal("5d8", p.PersonagemCombate.DadoVidaMaximo);

        Assert.Equal(2, p.PersonagemPericias.Count);
        Assert.Contains(p.PersonagemPericias, x => FichaPdfMapper.MesmaPericia(x.Pericia.Nome, "Atuação") && x.Bonus == 0);
        Assert.Contains(p.PersonagemPericias, x => FichaPdfMapper.MesmaPericia(x.Pericia.Nome, "Persuasão") && x.Bonus == 2);

        var ataque = Assert.Single(p.PersonagemAtaques);
        Assert.Equal("Rapieira", ataque.Nome);
        Assert.Equal("1d8", ataque.Dano);
        Assert.Equal("perfurante", ataque.TipoDano);
        Assert.Equal("Destreza", ataque.AtributoBase);
        Assert.True(ataque.Proficiente);
        Assert.Equal(0, ataque.BonusAcerto);
    }

    [Fact]
    public void Dinheiro_itens_detalhes_e_magias_voltam_certos()
    {
        var p = ExportarEImportar(Kaelith());

        Assert.Equal((1, 2, 3, 150, 4), (p.PersonagemDinheiro.PC, p.PersonagemDinheiro.PP, p.PersonagemDinheiro.PE, p.PersonagemDinheiro.PO, p.PersonagemDinheiro.PL));

        Assert.Equal(new[] { "Alaúde", "Adaga" }, p.PersonagemItens.Select(i => i.Nome));

        var d = p.PersonagemDetalhes;
        Assert.Equal("Inspiração de Bardo (d8)", d.CaracteristicasClasse);
        Assert.Equal("Visão no Escuro", d.TracosEspecie);
        Assert.Equal("Artista Nato", d.Talentos);
        Assert.Equal("Comum, Élfico", d.Idiomas);
        Assert.True(d.ProficienciaArmaduraLeve);
        Assert.False(d.ProficienciaArmaduraPesada);
        Assert.Equal("Armas simples, rapieiras", d.ProficienciaArmas);
        Assert.Equal("Cresceu em uma trupe de teatro.", d.HistoriaPersonalidade);

        var c = p.PersonagemConjuracao;
        Assert.Equal("Carisma", c.AtributoConjuracao);
        Assert.Equal((4, 3), (c.SlotsNivel1Max, c.SlotsNivel1Atual));
        Assert.Equal((3, 3), (c.SlotsNivel2Max, c.SlotsNivel2Atual));

        Assert.Contains(p.PersonagemMagias, m => m.Nome == "Zombaria Viciosa" && m.Nivel == 0);
        Assert.Contains(p.PersonagemMagias, m => m.Nome == "Curar Ferimentos" && m.Nivel == 1);
        Assert.Contains(p.PersonagemMagias, m => m.Nome == "Sugestão" && m.Nivel == 2);
    }

    [Fact]
    public void Iniciativa_diferente_do_modificador_e_preservada()
    {
        var original = Kaelith();
        original.PersonagemCombate.Iniciativa = 5;

        Assert.Equal(5, ExportarEImportar(original).PersonagemCombate.Iniciativa);
    }
}
