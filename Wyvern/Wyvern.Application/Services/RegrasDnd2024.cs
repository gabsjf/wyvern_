using System.Globalization;
using System.Text;

namespace Wyvern.Application.Services
{
    public enum TipoConjurador { Nenhum, Completo, Meio, Pacto }

    /// <summary>Um recurso de classe que cresce com o nível (Fúrias, Pontos de Foco, Ataque Furtivo...).</summary>
    public sealed record RecursoDeClasse(string Nome, Func<int, string?> Valor);

    public sealed record ClasseDnd(
        string Nome,
        int DadoVida,
        TipoConjurador Conjurador,
        int[] NiveisDeMelhoria,
        IReadOnlyDictionary<int, string[]> Caracteristicas,
        IReadOnlyList<RecursoDeClasse> Recursos,
        int[]? Truques = null,
        int[]? MagiasPreparadas = null);

    /// <summary>
    /// Tabelas de progressão do D&D 2024 (Livro do Jogador 2024 / SRD 5.2), níveis 1 a 20, uma classe só (sem multiclasse).
    /// Os nomes das características são traduções nossas; podem diferir dos da edição brasileira.
    /// </summary>
    public static class RegrasDnd2024
    {
        public const int NivelMaximo = 20;
        public const int NivelSubclasse = 3;
        public const int NivelDadivaEpica = 19;
        public const string Melhoria = "Melhoria de Atributo";
        public const string DadivaEpica = "Dádiva Épica";
        private const string Sub = "Característica de Subclasse";

        /// <summary>XP mínimo de cada nível (índice 0 = nível 1).</summary>
        public static readonly int[] XpPorNivel =
        {
            0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000,
            85000, 100000, 120000, 140000, 165000, 195000, 225000, 265000, 305000, 355000,
        };

        public static int Proficiencia(int nivel) => 2 + (Math.Clamp(nivel, 1, NivelMaximo) - 1) / 4;

        public static int NivelPeloXp(int xp)
        {
            var nivel = 1;
            for (var i = 1; i < XpPorNivel.Length; i++) if (xp >= XpPorNivel[i]) nivel = i + 1;
            return nivel;
        }

        /// <summary>Até que nível o personagem pode chegar agora: o maior entre o que o XP alcança e o marco do Mestre.</summary>
        public static int NivelDisponivel(int xp, int nivelLiberado) => Math.Min(NivelMaximo, Math.Max(NivelPeloXp(xp), nivelLiberado));

        /// <summary>PV fixos por nível (média arredondada para cima do dado de vida), antes do modificador de Constituição.</summary>
        public static int PvFixo(int dadoVida) => dadoVida / 2 + 1;

        public static int Modificador(int valor) => (int)Math.Floor((valor - 10) / 2.0);

        // ---- espaços de magia ----
        private static readonly int[][] EspacosCompletos =
        {
            new[] { 2 }, new[] { 3 }, new[] { 4, 2 }, new[] { 4, 3 }, new[] { 4, 3, 2 },
            new[] { 4, 3, 3 }, new[] { 4, 3, 3, 1 }, new[] { 4, 3, 3, 2 }, new[] { 4, 3, 3, 3, 1 }, new[] { 4, 3, 3, 3, 2 },
            new[] { 4, 3, 3, 3, 2, 1 }, new[] { 4, 3, 3, 3, 2, 1 }, new[] { 4, 3, 3, 3, 2, 1, 1 }, new[] { 4, 3, 3, 3, 2, 1, 1 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 1 }, new[] { 4, 3, 3, 3, 2, 1, 1, 1 }, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 },
            new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 }, new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 }, new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 },
        };

        // Paladino e Patrulheiro: no 2024 já têm espaços no 1º nível
        private static readonly int[][] EspacosMeio =
        {
            new[] { 2 }, new[] { 2 }, new[] { 3 }, new[] { 3 }, new[] { 4, 2 }, new[] { 4, 2 }, new[] { 4, 3 }, new[] { 4, 3 },
            new[] { 4, 3, 2 }, new[] { 4, 3, 2 }, new[] { 4, 3, 3 }, new[] { 4, 3, 3 }, new[] { 4, 3, 3, 1 }, new[] { 4, 3, 3, 1 },
            new[] { 4, 3, 3, 2 }, new[] { 4, 3, 3, 2 }, new[] { 4, 3, 3, 3, 1 }, new[] { 4, 3, 3, 3, 1 }, new[] { 4, 3, 3, 3, 2 }, new[] { 4, 3, 3, 3, 2 },
        };

        private static readonly int[] PactoQuantidade = { 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4 };
        private static readonly int[] PactoCirculo = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

        /// <summary>Espaços de magia máximos do 1º ao 9º círculo (sempre 9 posições).</summary>
        public static int[] Espacos(TipoConjurador tipo, int nivel)
        {
            var resultado = new int[9];
            var i = Math.Clamp(nivel, 1, NivelMaximo) - 1;
            switch (tipo)
            {
                case TipoConjurador.Completo: EspacosCompletos[i].CopyTo(resultado, 0); break;
                case TipoConjurador.Meio: EspacosMeio[i].CopyTo(resultado, 0); break;
                case TipoConjurador.Pacto: resultado[PactoCirculo[i] - 1] = PactoQuantidade[i]; break;
            }
            return resultado;
        }

        // ---- ajudantes para montar as tabelas ----
        private static Func<int, string?> Tabela(params int[] valores) => n => valores[n - 1] == 0 ? null : valores[n - 1].ToString();
        private static Func<int, string?> Faixas(params (int aPartirDe, string valor)[] faixas) =>
            n => faixas.LastOrDefault(f => n >= f.aPartirDe).valor is { Length: > 0 } v ? v : null;

        private static readonly int[] MelhoriaPadrao = { 4, 8, 12, 16 };
        private static readonly int[] PreparadasCompletas = { 4, 5, 6, 7, 9, 10, 11, 12, 14, 15, 16, 16, 17, 17, 18, 18, 19, 20, 21, 22 };
        private static readonly int[] PreparadasMeio = { 2, 3, 4, 5, 6, 6, 7, 7, 9, 9, 10, 10, 11, 11, 12, 12, 14, 14, 15, 15 };
        private static readonly int[] Truques2 = { 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
        private static readonly int[] Truques3 = { 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

        private static Dictionary<int, string[]> Niveis(params (int nivel, string[] nomes)[] itens) => itens.ToDictionary(i => i.nivel, i => i.nomes);
        private static (int, string[]) N(int nivel, params string[] nomes) => (nivel, nomes);

        public static readonly IReadOnlyDictionary<string, ClasseDnd> Classes = new Dictionary<string, ClasseDnd>
        {
            ["barbaro"] = new("Bárbaro", 12, TipoConjurador.Nenhum, MelhoriaPadrao, Niveis(
                N(1, "Fúria", "Defesa sem Armadura", "Maestria em Armas"), N(2, "Sentido de Perigo", "Ataque Imprudente"),
                N(3, "Subclasse de Bárbaro", "Conhecimento Primitivo"), N(5, "Ataque Extra", "Movimento Rápido"), N(6, Sub),
                N(7, "Instinto Selvagem", "Bote Instintivo"), N(9, "Golpe Brutal"), N(10, Sub), N(11, "Fúria Implacável"),
                N(13, "Golpe Brutal Aprimorado"), N(14, Sub), N(15, "Fúria Persistente"), N(17, "Golpe Brutal Aprimorado"),
                N(18, "Poder Indomável"), N(20, "Campeão Primitivo")),
                new[]
                {
                    new RecursoDeClasse("Fúrias", Tabela(2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6)),
                    new RecursoDeClasse("Dano de Fúria", Faixas((1, "+2"), (9, "+3"), (16, "+4"))),
                    new RecursoDeClasse("Maestria em Armas", Faixas((1, "2"), (4, "3"), (10, "4"))),
                }),

            ["bardo"] = new("Bardo", 8, TipoConjurador.Completo, MelhoriaPadrao, Niveis(
                N(1, "Inspiração de Bardo", "Conjuração"), N(2, "Especialização", "Pau para Toda Obra"), N(3, "Subclasse de Bardo"),
                N(5, "Fonte de Inspiração"), N(6, Sub), N(7, "Contracanto"), N(9, "Especialização"), N(10, "Segredos Mágicos"),
                N(14, Sub), N(18, "Inspiração Superior"), N(20, "Palavras da Criação")),
                new[] { new RecursoDeClasse("Dado de Bardo", Faixas((1, "d6"), (5, "d8"), (10, "d10"), (15, "d12"))) },
                Truques2, PreparadasCompletas),

            ["clerigo"] = new("Clérigo", 8, TipoConjurador.Completo, MelhoriaPadrao, Niveis(
                N(1, "Conjuração", "Ordem Divina"), N(2, "Canalizar Divindade"), N(3, "Subclasse de Clérigo"), N(5, "Abrasar Mortos-Vivos"),
                N(6, Sub), N(7, "Golpes Abençoados"), N(10, "Intervenção Divina"), N(14, "Golpes Abençoados Aprimorados"),
                N(17, Sub), N(20, "Intervenção Divina Maior")),
                new[] { new RecursoDeClasse("Canalizar Divindade", Tabela(0, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4)) },
                Truques3, PreparadasCompletas),

            ["druida"] = new("Druida", 8, TipoConjurador.Completo, MelhoriaPadrao, Niveis(
                N(1, "Conjuração", "Druídico", "Ordem Primitiva"), N(2, "Forma Selvagem", "Companheiro Selvagem"), N(3, "Subclasse de Druida"),
                N(5, "Ressurgimento Selvagem"), N(6, Sub), N(7, "Fúria Elemental"), N(10, Sub), N(14, Sub),
                N(15, "Fúria Elemental Aprimorada"), N(18, "Magias de Fera"), N(20, "Arquidruida")),
                new[] { new RecursoDeClasse("Forma Selvagem", Tabela(0, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4)) },
                Truques2, PreparadasCompletas),

            ["guerreiro"] = new("Guerreiro", 10, TipoConjurador.Nenhum, new[] { 4, 6, 8, 12, 14, 16 }, Niveis(
                N(1, "Estilo de Luta", "Retomar o Fôlego", "Maestria em Armas"), N(2, "Surto de Ação (1 uso)", "Mente Tática"),
                N(3, "Subclasse de Guerreiro"), N(5, "Ataque Extra", "Mudança Tática"), N(7, Sub),
                N(9, "Indomável (1 uso)", "Mestre Tático"), N(10, Sub), N(11, "Dois Ataques Extras"),
                N(13, "Indomável (2 usos)", "Ataques Estudados"), N(15, Sub), N(17, "Surto de Ação (2 usos)", "Indomável (3 usos)"),
                N(18, Sub), N(20, "Três Ataques Extras")),
                new[]
                {
                    new RecursoDeClasse("Retomar o Fôlego", Faixas((1, "2"), (4, "3"), (10, "4"))),
                    new RecursoDeClasse("Maestria em Armas", Faixas((1, "3"), (4, "4"), (10, "5"), (16, "6"))),
                }),

            ["monge"] = new("Monge", 8, TipoConjurador.Nenhum, MelhoriaPadrao, Niveis(
                N(1, "Artes Marciais", "Defesa sem Armadura"), N(2, "Foco do Monge", "Movimento sem Armadura", "Metabolismo Extraordinário"),
                N(3, "Desviar Ataques", "Subclasse de Monge"), N(4, "Queda Lenta"), N(5, "Ataque Extra", "Golpe Atordoante"),
                N(6, "Golpes Fortalecidos", Sub), N(7, "Evasão"), N(9, "Movimento Acrobático"), N(10, "Foco Elevado", "Autorrestauração"),
                N(11, Sub), N(13, "Desviar Energia"), N(14, "Sobrevivente Disciplinado"), N(15, "Foco Perfeito"), N(17, Sub),
                N(18, "Defesa Superior"), N(20, "Corpo e Mente")),
                new[]
                {
                    new RecursoDeClasse("Dado de Artes Marciais", Faixas((1, "1d6"), (5, "1d8"), (11, "1d10"), (17, "1d12"))),
                    new RecursoDeClasse("Pontos de Foco", n => n < 2 ? null : n.ToString()),
                    new RecursoDeClasse("Movimento sem Armadura", Faixas((1, ""), (2, "+3 m"), (6, "+4,5 m"), (10, "+6 m"), (14, "+7,5 m"), (18, "+9 m"))),
                }),

            ["paladino"] = new("Paladino", 10, TipoConjurador.Meio, MelhoriaPadrao, Niveis(
                N(1, "Imposição das Mãos", "Conjuração", "Maestria em Armas"), N(2, "Estilo de Luta", "Golpe do Paladino"),
                N(3, "Canalizar Divindade", "Subclasse de Paladino"), N(5, "Ataque Extra", "Corcel Fiel"), N(6, "Aura de Proteção"),
                N(7, Sub), N(9, "Abjurar Inimigos"), N(10, "Aura de Coragem"), N(11, "Golpes Radiantes"), N(14, "Toque Restaurador"),
                N(15, Sub), N(18, "Expansão de Aura"), N(20, Sub)),
                new[]
                {
                    new RecursoDeClasse("Canalizar Divindade", Tabela(0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3)),
                    new RecursoDeClasse("Imposição das Mãos (PV)", n => (5 * n).ToString()),
                }, null, PreparadasMeio),

            ["patrulheiro"] = new("Patrulheiro", 10, TipoConjurador.Meio, MelhoriaPadrao, Niveis(
                N(1, "Conjuração", "Inimigo Favorito", "Maestria em Armas"), N(2, "Explorador Hábil", "Estilo de Luta"),
                N(3, "Subclasse de Patrulheiro"), N(5, "Ataque Extra"), N(6, "Andarilho"), N(7, Sub), N(9, "Especialização"),
                N(10, "Incansável"), N(11, Sub), N(13, "Caçador Implacável"), N(14, "Véu da Natureza"), N(15, Sub),
                N(17, "Caçador Preciso"), N(18, "Sentidos Selvagens"), N(20, "Matador de Inimigos")),
                new[] { new RecursoDeClasse("Inimigo Favorito", Tabela(2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6)) },
                null, new[] { 2, 3, 4, 5, 6, 6, 7, 7, 8, 8, 10, 10, 11, 11, 12, 12, 14, 14, 15, 15 }),

            ["ladino"] = new("Ladino", 8, TipoConjurador.Nenhum, new[] { 4, 8, 10, 12, 16 }, Niveis(
                N(1, "Especialização", "Ataque Furtivo", "Gíria de Ladrão", "Maestria em Armas"), N(2, "Ação Ardilosa"),
                N(3, "Subclasse de Ladino", "Mira Firme"), N(5, "Golpe Ardiloso", "Esquiva Sobrenatural"), N(6, "Especialização"),
                N(7, "Evasão", "Talento Confiável"), N(9, Sub), N(11, "Golpe Ardiloso Aprimorado"), N(13, Sub),
                N(14, "Golpes Traiçoeiros"), N(15, "Mente Escorregadia"), N(17, Sub), N(18, "Elusivo"), N(20, "Golpe de Sorte")),
                new[] { new RecursoDeClasse("Ataque Furtivo", n => $"{(n + 1) / 2}d6") }),

            ["feiticeiro"] = new("Feiticeiro", 6, TipoConjurador.Completo, MelhoriaPadrao, Niveis(
                N(1, "Conjuração", "Feitiçaria Inata"), N(2, "Fonte de Magia", "Metamagia"), N(3, "Subclasse de Feiticeiro"),
                N(5, "Restauração Feiticeira"), N(6, Sub), N(7, "Feitiçaria Encarnada"), N(10, "Metamagia"), N(14, Sub),
                N(17, "Metamagia"), N(18, Sub), N(20, "Apoteose Arcana")),
                new[] { new RecursoDeClasse("Pontos de Feitiçaria", n => n < 2 ? null : n.ToString()) },
                new[] { 4, 4, 4, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 },
                new[] { 2, 4, 6, 7, 9, 10, 11, 12, 14, 15, 16, 16, 17, 17, 18, 18, 19, 20, 21, 22 }),

            ["bruxo"] = new("Bruxo", 8, TipoConjurador.Pacto, MelhoriaPadrao, Niveis(
                N(1, "Invocações Místicas", "Magia de Pacto"), N(2, "Astúcia Mágica"), N(3, "Subclasse de Bruxo"), N(6, Sub),
                N(9, "Contatar Patrono"), N(10, Sub), N(11, "Arcano Místico (6º círculo)"), N(13, "Arcano Místico (7º círculo)"),
                N(14, Sub), N(15, "Arcano Místico (8º círculo)"), N(17, "Arcano Místico (9º círculo)"), N(20, "Mestre Místico")),
                new[] { new RecursoDeClasse("Invocações Místicas", Tabela(1, 3, 3, 3, 5, 5, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10)) },
                Truques2, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15 }),

            ["mago"] = new("Mago", 6, TipoConjurador.Completo, MelhoriaPadrao, Niveis(
                N(1, "Conjuração", "Adepto de Rituais", "Recuperação Arcana"), N(2, "Estudioso"), N(3, "Subclasse de Mago"),
                N(5, "Memorizar Magia"), N(6, Sub), N(10, Sub), N(14, Sub), N(18, "Domínio de Magia"), N(20, "Magias Assinatura")),
                Array.Empty<RecursoDeClasse>(),
                Truques3, new[] { 4, 5, 6, 7, 9, 10, 11, 12, 14, 15, 16, 16, 17, 18, 19, 21, 22, 23, 24, 25 }),
        };

        /// <summary>Nomes que a ficha pode trazer (português, inglês, com ou sem acento) apontando para a chave da classe.</summary>
        private static readonly Dictionary<string, string> Apelidos = new()
        {
            ["barbaro"] = "barbaro", ["barbarian"] = "barbaro",
            ["bardo"] = "bardo", ["bard"] = "bardo",
            ["clerigo"] = "clerigo", ["cleric"] = "clerigo",
            ["druida"] = "druida", ["druid"] = "druida",
            ["guerreiro"] = "guerreiro", ["fighter"] = "guerreiro", ["lutador"] = "guerreiro",
            ["monge"] = "monge", ["monk"] = "monge",
            ["paladino"] = "paladino", ["paladin"] = "paladino",
            ["patrulheiro"] = "patrulheiro", ["ranger"] = "patrulheiro", ["guardiao"] = "patrulheiro",
            ["ladino"] = "ladino", ["rogue"] = "ladino", ["ladrao"] = "ladino",
            ["feiticeiro"] = "feiticeiro", ["sorcerer"] = "feiticeiro",
            ["bruxo"] = "bruxo", ["warlock"] = "bruxo",
            ["mago"] = "mago", ["wizard"] = "mago",
        };

        /// <summary>Acha a classe pelo texto da ficha ("Guerreiro", "fighter", "Clérigo (Vida)"...). Nulo quando não é uma classe do livro.</summary>
        public static ClasseDnd? AcharClasse(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var palavra = SemAcento(texto).ToLowerInvariant().Split(new[] { ' ', '(', '/', '-', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return palavra != null && Apelidos.TryGetValue(palavra, out var chave) ? Classes[chave] : null;
        }

        public static bool TemMelhoria(ClasseDnd classe, int nivel) => classe.NiveisDeMelhoria.Contains(nivel);

        /// <summary>Características ganhas ao chegar no nível, incluindo Melhoria de Atributo e Dádiva Épica.</summary>
        public static List<string> CaracteristicasDoNivel(ClasseDnd classe, int nivel)
        {
            var lista = new List<string>(classe.Caracteristicas.TryGetValue(nivel, out var nomes) ? nomes : Array.Empty<string>());
            if (TemMelhoria(classe, nivel)) lista.Add(Melhoria);
            if (nivel == NivelDadivaEpica) lista.Add(DadivaEpica);
            return lista;
        }

        private static string SemAcento(string texto)
        {
            var normal = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normal.Length);
            foreach (var c in normal)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
