using Wyvern.Application.Services;
using Wyvern.Domain.Entities;

namespace Wyvern.Tests;

/// <summary>
/// WYV-21: campos que saíam vazios ou no lugar errado ao exportar e importar a ficha em PDF.
/// </summary>
public class FichaPdfCorrecoesTests
{
    private static string Modelo => Path.Combine(AppContext.BaseDirectory, "Templates", "ficha55.pdf");

    private static Personagem ExportarEImportar(Personagem original)
    {
        var bytes = new PdfExportService().ExportPdf(original, Modelo);
        using var ms = new MemoryStream(bytes);
        return new PdfParserService().ParsePdf(ms, campanhaId: 7, criadoPorId: 9);
    }

    private static Personagem Lyra() => new()
    {
        Nome = "Lyra",
        CriadoPor = new Usuario { Nome = "Ana Souza" },
        Atributo = new Atributo { Forca = 8, Destreza = 16, Constituicao = 14, Inteligencia = 18, Sabedoria = 12, Carisma = 10 },
        PersonagemPlayer = new PersonagemPlayer { Classe = "Maga", Nivel = 5, Raca = "Elfa" },
        PersonagemCombate = new PersonagemCombate { VidaMaxima = 27, VidaAtual = 20, ProficienciaBonus = 3, DadoVidaMaximo = "5d6", DadoVidaGasto = 2 },
        PersonagemItens = new List<PersonagemItem>
        {
            new() { Nome = "Grimório", Raridade = "Comum" },
            new() { Nome = "Anel de Proteção", Raridade = "Raro" },
        },
        PersonagemAtaques = Enumerable.Range(1, 5)
            .Select(i => new PersonagemAtaque { Nome = $"Arma {i}", AtributoBase = "Destreza", Proficiente = true, Dano = "1d6", TipoDano = "perfurante" })
            .ToList(),
        PersonagemDetalhes = new PersonagemDetalhes
        {
            CaracteristicasClasse = "Recuperação Arcana",
            Talentos = "Iniciado em Magia",
            HistoriaPersonalidade = string.Join("\n",
                "Traços: Curiosa demais.",
                "Ideais: Conhecimento acima de tudo.",
                "Vínculos: O grimório do mestre.",
                "Defeitos: Não confia em ninguém.",
                "História: Cresceu na torre de Vael.",
                "Fugiu na noite do incêndio."),
        },
    };

    // ---------- atributos: o quadrado grande é o modificador, o oval é a pontuação ----------

    [Fact]
    public void Exporta_o_modificador_no_quadrado_e_a_pontuacao_no_oval()
    {
        var t = FichaPdfMapper.Mapear(Lyra()).Textos;

        Assert.Equal("+4", t["INT"]);
        Assert.Equal("18", t["INTmod"]);
        Assert.Equal("-1", t["STR"]);
        Assert.Equal("8", t["STRmod"]);
    }

    [Theory]
    [InlineData("+3", "16", 16)]   // ficha oficial (e a nossa exportação nova)
    [InlineData("16", "+3", 16)]   // exportação antiga: pontuação no quadrado
    [InlineData("16", "3", 16)]    // sem sinal nos dois: a pontuação é a que tem o outro como modificador
    [InlineData("3", "16", 16)]
    [InlineData("-1", "", 8)]      // só o modificador: a menor pontuação com esse modificador
    [InlineData("", "", 10)]
    [InlineData("12", "", 12)]
    [InlineData("abc", "40", 10)]  // nada aproveitável
    public void Importa_a_pontuacao_de_qualquer_um_dos_dois_campos(string quadrado, string oval, int esperado)
    {
        Assert.Equal(esperado, FichaPdfLeitor.EscolherPontuacao(quadrado, oval));
    }

    [Fact]
    public void Ficha_preenchida_do_jeito_oficial_importa_os_atributos_certos()
    {
        var textos = new Dictionary<string, string>
        {
            ["CharacterName"] = "Thorin", ["ClassLevel"] = "Guerreiro 3",
            ["STR"] = "+3", ["STRmod"] = "16", ["DEX"] = "+1", ["DEXmod"] = "12", ["CON"] = "+2", ["CONmod"] = "14",
            ["INT"] = "-1", ["INTmod"] = "8", ["WIS"] = "+0", ["WISmod"] = "10", ["CHA"] = "+1", ["CHamod"] = "13",
        };

        var a = FichaPdfLeitor.Ler(textos, new Dictionary<string, bool>(), 1, 1).Atributo;

        Assert.Equal((16, 12, 14, 8, 10, 13), (a.Forca, a.Destreza, a.Constituicao, a.Inteligencia, a.Sabedoria, a.Carisma));
    }

    // ---------- campos que saíam vazios ----------

    [Fact]
    public void Exporta_personalidade_dados_de_vida_restantes_e_nome_do_jogador()
    {
        var t = FichaPdfMapper.Mapear(Lyra()).Textos;

        Assert.Equal("Curiosa demais.", t["PersonalityTraits"]);
        Assert.Equal("Conhecimento acima de tudo.", t["Ideals"]);
        Assert.Equal("O grimório do mestre.", t["Bonds"]);
        Assert.Equal("Não confia em ninguém.", t["Flaws"]);
        Assert.Equal("3d6", t["HD"]);          // 5 dados, 2 gastos
        Assert.Equal("5d6", t["HDTotal"]);
        Assert.Equal("Ana Souza", t["PlayerName"]);
    }

    [Fact]
    public void A_historia_vai_para_a_pagina_2_com_as_quebras_de_linha_e_sem_as_secoes_de_personalidade()
    {
        var backstory = FichaPdfMapper.Mapear(Lyra()).Textos["Backstory"];

        Assert.Equal("Cresceu na torre de Vael.\nFugiu na noite do incêndio.", backstory);
    }

    [Fact]
    public void Talentos_ficam_so_na_pagina_2_e_equipamento_sem_o_comum_repetido()
    {
        var t = FichaPdfMapper.Mapear(Lyra()).Textos;

        Assert.Equal("Iniciado em Magia", t["Feat+Traits"]);
        Assert.DoesNotContain("Iniciado em Magia", t["Features and Traits"]);
        Assert.Equal("• Grimório\n• Anel de Proteção (Raro)", t["Equipment"]);
    }

    [Theory]
    [InlineData("5d8", 0, "5d8")]
    [InlineData("5d8", 5, "0d8")]
    [InlineData("d10", 1, "4d10")]   // sem número: o total é o nível (5)
    public void Dados_de_vida_restantes(string dado, int gastos, string esperado)
    {
        var p = new Personagem
        {
            PersonagemPlayer = new PersonagemPlayer { Nivel = 5 },
            PersonagemCombate = new PersonagemCombate { DadoVidaMaximo = dado, DadoVidaGasto = gastos },
        };
        Assert.Equal(esperado, FichaPdfMapper.DadosDeVidaRestantes(p));
    }

    [Theory]
    [InlineData("5d8", "3d8", 5, 2)]
    [InlineData("5d8", "", 5, 0)]     // quadro vazio: nada gasto
    [InlineData("", "1", 4, 3)]       // sem total: usa o nível
    [InlineData("5d8", "9d8", 5, 0)]  // mais restantes que o total: nada gasto
    public void Importa_os_dados_de_vida_gastos(string total, string restantes, int nivel, int esperado)
    {
        Assert.Equal(esperado, FichaPdfLeitor.DadosDeVidaGastos(total, restantes, nivel));
    }

    // ---------- ida e volta pelo PDF de verdade ----------

    [Fact]
    public void Ida_e_volta_preserva_personalidade_historia_em_varias_linhas_e_dados_de_vida()
    {
        var p = ExportarEImportar(Lyra());

        var historia = HistoriaEmSecoes.Ler(p.PersonagemDetalhes.HistoriaPersonalidade);
        Assert.Equal("Curiosa demais.", historia.De(HistoriaEmSecoes.Tracos));
        Assert.Equal("Não confia em ninguém.", historia.De(HistoriaEmSecoes.Defeitos));
        Assert.Equal("Cresceu na torre de Vael.\nFugiu na noite do incêndio.", historia.De(HistoriaEmSecoes.Historia));
        Assert.Equal("Iniciado em Magia", p.PersonagemDetalhes.Talentos);
        Assert.Equal(2, p.PersonagemCombate.DadoVidaGasto);
        Assert.Equal((8, 16, 14, 18), (p.Atributo.Forca, p.Atributo.Destreza, p.Atributo.Constituicao, p.Atributo.Inteligencia));
    }

    [Fact]
    public void Ida_e_volta_preserva_os_ataques_alem_do_terceiro()
    {
        var p = ExportarEImportar(Lyra());

        Assert.Equal(new[] { "Arma 1", "Arma 2", "Arma 3", "Arma 4", "Arma 5" }, p.PersonagemAtaques.Select(a => a.Nome));
        Assert.All(p.PersonagemAtaques, a =>
        {
            Assert.Equal("1d6", a.Dano);
            Assert.Equal("perfurante", a.TipoDano);
            Assert.Equal("Destreza", a.AtributoBase);
            Assert.True(a.Proficiente);
        });
    }

    [Fact]
    public void Ida_e_volta_mantem_a_raridade_so_onde_ela_existia()
    {
        var itens = ExportarEImportar(Lyra()).PersonagemItens;
        Assert.Contains(itens, i => i.Nome == "Grimório" && i.Raridade == "Comum");
        Assert.Contains(itens, i => i.Nome == "Anel de Proteção" && i.Raridade == "Raro");
    }

    // ---------- seções da história ----------

    [Fact]
    public void Secoes_da_historia_aceitam_varias_linhas_e_texto_livre_antes()
    {
        var r = HistoriaEmSecoes.Ler("Uma introdução.\nTraços de Espécie: Visão no escuro\nTraços: Calma\nmesmo em combate\nHistória: Linha 1\n\nLinha 2");

        Assert.Equal("Uma introdução.", r.Livre);
        Assert.Equal("Visão no escuro", r.De(HistoriaEmSecoes.TracosDeEspecie));
        Assert.Equal("Calma\nmesmo em combate", r.De(HistoriaEmSecoes.Tracos));
        Assert.Equal("Linha 1\n\nLinha 2", r.De(HistoriaEmSecoes.Historia));
    }

    [Fact]
    public void Texto_sem_secoes_e_todo_livre_e_montar_pula_o_que_esta_vazio()
    {
        Assert.Equal("Só uma história.", HistoriaEmSecoes.Ler("Só uma história.").Livre);
        Assert.False(HistoriaEmSecoes.Ler("Só uma história.").TemSecoes);
        Assert.Equal("Ideais: Honra\nHistória: Nasceu.", HistoriaEmSecoes.Montar(null, (HistoriaEmSecoes.Tracos, ""), (HistoriaEmSecoes.Ideais, "Honra"), (HistoriaEmSecoes.Historia, "Nasceu.")));
    }
}
