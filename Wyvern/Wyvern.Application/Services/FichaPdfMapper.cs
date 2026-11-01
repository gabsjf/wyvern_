using System.Globalization;
using System.Text;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Regras;

namespace Wyvern.Application.Services
{
    /// <summary>Valores a escrever no PDF: texto por campo e caixas de marcar (true = marcada).</summary>
    public record FichaPreenchida(
        IReadOnlyDictionary<string, string> Textos,
        IReadOnlyDictionary<string, bool> Caixas);

    /// <summary>
    /// Converte um personagem nos campos da ficha padrão de D&amp;D 5e (Templates/ficha55.pdf, 3 páginas).
    /// É uma função pura: nada de PDF aqui, só "campo → valor", o que permite testar sem abrir arquivo.
    /// Os nomes seguem o modelo; espaços sobrando (ex.: "Race ") são tratados na hora de escrever.
    /// </summary>
    public static class FichaPdfMapper
    {
        internal record PericiaPdf(string[] Nomes, string Campo, string Caixa, string Atributo);

        // Nomes em português (como no cadastro de perícias) e o campo/caixa correspondente no modelo.
        internal static readonly PericiaPdf[] Pericias =
        {
            new(new[] { "acrobacia" }, "Acrobatics", "Check Box 23", "des"),
            new(new[] { "lidar com animais", "adestrar animais" }, "Animal", "Check Box 24", "sab"),
            new(new[] { "arcanismo" }, "Arcana", "Check Box 25", "int"),
            new(new[] { "atletismo" }, "Athletics", "Check Box 26", "for"),
            new(new[] { "enganacao" }, "Deception", "Check Box 27", "car"),
            new(new[] { "historia" }, "History", "Check Box 28", "int"),
            new(new[] { "intuicao" }, "Insight", "Check Box 29", "sab"),
            new(new[] { "intimidacao" }, "Intimidation", "Check Box 30", "car"),
            new(new[] { "investigacao" }, "Investigation", "Check Box 31", "int"),
            new(new[] { "medicina" }, "Medicine", "Check Box 32", "sab"),
            new(new[] { "natureza" }, "Nature", "Check Box 33", "int"),
            new(new[] { "percepcao" }, "Perception", "Check Box 34", "sab"),
            new(new[] { "atuacao" }, "Performance", "Check Box 35", "car"),
            new(new[] { "persuasao" }, "Persuasion", "Check Box 36", "car"),
            new(new[] { "religiao" }, "Religion", "Check Box 37", "int"),
            new(new[] { "prestidigitacao" }, "SleightofHand", "Check Box 38", "des"),
            new(new[] { "furtividade" }, "Stealth", "Check Box 39", "des"),
            new(new[] { "sobrevivencia" }, "Survival", "Check Box 40", "sab"),
        };

        // Salvaguardas: campo do valor e caixa de proficiência (na ordem FOR, DES, CON, INT, SAB, CAR).
        internal static readonly (string Atributo, string Campo, string Caixa)[] Salvaguardas =
        {
            ("for", "ST Strength", "Check Box 11"),
            ("des", "ST Dexterity", "Check Box 18"),
            ("con", "ST Constitution", "Check Box 19"),
            ("int", "ST Intelligence", "Check Box 20"),
            ("sab", "ST Wisdom", "Check Box 21"),
            ("car", "ST Charisma", "Check Box 22"),
        };

        internal static readonly (string Atributo, string Pontuacao, string Modificador)[] CamposAtributos =
        {
            ("for", "STR", "STRmod"),
            ("des", "DEX", "DEXmod"),
            ("con", "CON", "CONmod"),
            ("int", "INT", "INTmod"),
            ("sab", "WIS", "WISmod"),
            ("car", "CHA", "CHamod"),
        };

        // Linhas de magia da página 3, de cima para baixo, por nível (0 = truques).
        internal static readonly Dictionary<int, string[]> LinhasDeMagia = new()
        {
            [0] = new[] { "Spells 1014", "Spells 1016", "Spells 1017", "Spells 1018", "Spells 1019", "Spells 1020", "Spells 1021", "Spells 1022" },
            [1] = new[] { "Spells 1015", "Spells 1023", "Spells 1024", "Spells 1025", "Spells 1026", "Spells 1027", "Spells 1028", "Spells 1029", "Spells 1030", "Spells 1031", "Spells 1032", "Spells 1033" },
            [2] = new[] { "Spells 1046", "Spells 1034", "Spells 1035", "Spells 1036", "Spells 1037", "Spells 1038", "Spells 1039", "Spells 1040", "Spells 1041", "Spells 1042", "Spells 1043", "Spells 1044", "Spells 1045" },
            [3] = new[] { "Spells 1048", "Spells 1047", "Spells 1049", "Spells 1050", "Spells 1051", "Spells 1052", "Spells 1053", "Spells 1054", "Spells 1055", "Spells 1056", "Spells 1057", "Spells 1058", "Spells 1059" },
            [4] = new[] { "Spells 1061", "Spells 1060", "Spells 1062", "Spells 1063", "Spells 1064", "Spells 1065", "Spells 1066", "Spells 1067", "Spells 1068", "Spells 1069", "Spells 1070", "Spells 1071", "Spells 1072" },
            [5] = new[] { "Spells 1074", "Spells 1073", "Spells 1075", "Spells 1076", "Spells 1077", "Spells 1078", "Spells 1079", "Spells 1080", "Spells 1081" },
            [6] = new[] { "Spells 1083", "Spells 1082", "Spells 1084", "Spells 1085", "Spells 1086", "Spells 1087", "Spells 1088", "Spells 1089", "Spells 1090" },
            [7] = new[] { "Spells 1092", "Spells 1091", "Spells 1093", "Spells 1094", "Spells 1095", "Spells 1096", "Spells 1097", "Spells 1098", "Spells 1099" },
            [8] = new[] { "Spells 10101", "Spells 10100", "Spells 10102", "Spells 10103", "Spells 10104", "Spells 10105", "Spells 10106" },
            [9] = new[] { "Spells 10108", "Spells 10107", "Spells 10109", "Spells 101010", "Spells 101011", "Spells 101012", "Spells 101013" },
        };

        public static FichaPreenchida Mapear(Personagem p)
        {
            var t = new Dictionary<string, string>();
            var c = new Dictionary<string, bool>();

            var atributo = p.Atributo;
            var combate = p.PersonagemCombate;
            var jogador = p.PersonagemPlayer;
            var detalhes = p.PersonagemDetalhes;
            var bonusProf = combate?.ProficienciaBonus ?? 2;

            int? Pontuacao(string codigo) => PontuacaoDe(atributo, codigo);
            int Mod(string codigo) => Pontuacao(codigo) is int v ? RegrasDnd.Modificador(v) : 0;

            // ---- Página 1: identificação ----
            Definir(t, "CharacterName", p.Nome);
            Definir(t, "CharacterName 2", p.Nome);

            if (jogador != null)
            {
                var classe = (jogador.Classe ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(jogador.Subclasse)) classe += $" ({jogador.Subclasse.Trim()})";
                Definir(t, "ClassLevel", $"{classe} {jogador.Nivel}".Trim());
                Definir(t, "Background", jogador.Antecedente);
                Definir(t, "Race", jogador.Raca);
                Definir(t, "Alignment", jogador.Alinhamento);
                Definir(t, "XP", jogador.Xp.ToString(CultureInfo.InvariantCulture));
            }

            // ---- Atributos ----
            if (atributo != null)
            {
                foreach (var (codigo, campoPontuacao, campoMod) in CamposAtributos)
                {
                    var pontuacao = Pontuacao(codigo)!.Value;
                    Definir(t, campoPontuacao, pontuacao.ToString(CultureInfo.InvariantCulture));
                    Definir(t, campoMod, RegrasDnd.ComSinal(RegrasDnd.Modificador(pontuacao)));
                }

                foreach (var (codigo, campo, caixa) in Salvaguardas)
                {
                    var proficiente = SalvaguardaProficiente(atributo, codigo);
                    Definir(t, campo, RegrasDnd.ComSinal(Mod(codigo) + (proficiente ? bonusProf : 0)));
                    c[caixa] = proficiente;
                }
            }

            // ---- Combate ----
            if (combate != null)
            {
                Definir(t, "ProfBonus", RegrasDnd.ComSinal(bonusProf));
                Definir(t, "AC", combate.ClasseArmadura.ToString(CultureInfo.InvariantCulture));
                var iniciativa = combate.Iniciativa != 0 ? combate.Iniciativa : Mod("des");
                Definir(t, "Initiative", RegrasDnd.ComSinal(iniciativa));
                Definir(t, "Speed", combate.Deslocamento);
                Definir(t, "HPMax", combate.VidaMaxima.ToString(CultureInfo.InvariantCulture));
                Definir(t, "HPCurrent", combate.VidaAtual.ToString(CultureInfo.InvariantCulture));
                if (combate.VidaTemporaria > 0) Definir(t, "HPTemp", combate.VidaTemporaria.ToString(CultureInfo.InvariantCulture));
                Definir(t, "HDTotal", combate.DadoVidaMaximo);
                if (combate.InspiracaoHeroica) Definir(t, "Inspiration", "1");

                for (var i = 1; i <= 3; i++)
                {
                    c[$"Check Box {11 + i}"] = combate.DeathSaveSucessos >= i;  // 12, 13, 14
                    c[$"Check Box {14 + i}"] = combate.DeathSaveFalhas >= i;    // 15, 16, 17
                }
            }

            // ---- Perícias (sempre as 18, com o total calculado) ----
            var cadastradas = (p.PersonagemPericias ?? new List<PersonagemPericia>())
                .Where(x => x.Pericia != null)
                .ToDictionary(x => Normalizar(x.Pericia.Nome), x => x, StringComparer.Ordinal);

            var percepcao = 0;
            foreach (var pericia in Pericias)
            {
                var cadastro = pericia.Nomes.Select(n => cadastradas.GetValueOrDefault(n)).FirstOrDefault(x => x != null);
                // No sistema, a perícia estar na lista do personagem já significa "proficiente" (é assim que a ficha na tela
                // calcula); o campo Proficiente da tabela de ligação não é preenchido pelo cadastro.
                var proficiente = cadastro != null;
                var total = Mod(pericia.Atributo) + (proficiente ? bonusProf : 0) + (cadastro?.Bonus ?? 0);

                Definir(t, pericia.Campo, RegrasDnd.ComSinal(total));
                c[pericia.Caixa] = proficiente;
                if (pericia.Campo == "Perception") percepcao = total;
            }
            Definir(t, "Passive", (10 + percepcao).ToString(CultureInfo.InvariantCulture));

            // ---- Ataques: 3 linhas na ficha; o que passar disso vai para o quadro de texto ----
            var ataques = (p.PersonagemAtaques ?? new List<PersonagemAtaque>()).ToList();
            var nomesAtaque = new[] { "Wpn Name", "Wpn Name 2", "Wpn Name 3" };
            var bonusAtaque = new[] { "Wpn1 AtkBonus", "Wpn2 AtkBonus", "Wpn3 AtkBonus" };
            var danoAtaque = new[] { "Wpn1 Damage", "Wpn2 Damage", "Wpn3 Damage" };

            for (var i = 0; i < ataques.Count; i++)
            {
                var (bonus, dano) = CalcularAtaque(ataques[i], atributo, bonusProf);
                var textoDano = $"{dano} {ataques[i].TipoDano}".Trim();

                if (i < 3)
                {
                    Definir(t, nomesAtaque[i], ataques[i].Nome);
                    Definir(t, bonusAtaque[i], bonus);
                    Definir(t, danoAtaque[i], textoDano);
                }
            }

            var extras = ataques.Skip(3).Select(a =>
            {
                var (bonus, dano) = CalcularAtaque(a, atributo, bonusProf);
                return $"{a.Nome} {bonus}, {dano} {a.TipoDano}".Trim().TrimEnd(',');
            });
            Definir(t, "AttacksSpellcasting", string.Join("\n", extras));

            // ---- Dinheiro, equipamento, características ----
            var dinheiro = p.PersonagemDinheiro;
            if (dinheiro != null)
            {
                Definir(t, "CP", dinheiro.PC.ToString(CultureInfo.InvariantCulture));
                Definir(t, "SP", dinheiro.PP.ToString(CultureInfo.InvariantCulture));
                Definir(t, "EP", dinheiro.PE.ToString(CultureInfo.InvariantCulture));
                Definir(t, "GP", dinheiro.PO.ToString(CultureInfo.InvariantCulture));
                Definir(t, "PP", dinheiro.PL.ToString(CultureInfo.InvariantCulture));
            }

            Definir(t, "Equipment", string.Join("\n", (p.PersonagemItens ?? new List<PersonagemItem>())
                .Select(i => "• " + i.Nome + (string.IsNullOrWhiteSpace(i.Raridade) ? "" : $" ({i.Raridade})"))));

            if (detalhes != null)
            {
                var historia = detalhes.HistoriaPersonalidade ?? "";
                var tracos = Primeiro(detalhes.TracosEspecie, ExtrairSecao(historia, "Traços de Espécie:"));
                var talentos = Primeiro(detalhes.Talentos, ExtrairSecao(historia, "Talentos:"));

                var caracteristicas = new List<string>();
                if (!string.IsNullOrWhiteSpace(detalhes.CaracteristicasClasse)) caracteristicas.Add(detalhes.CaracteristicasClasse.Trim());
                if (!string.IsNullOrWhiteSpace(tracos)) caracteristicas.Add("Traços de espécie:\n" + tracos);
                if (!string.IsNullOrWhiteSpace(talentos)) caracteristicas.Add("Talentos:\n" + talentos);
                Definir(t, "Features and Traits", string.Join("\n\n", caracteristicas));

                var proficiencias = new List<string>();
                if (!string.IsNullOrWhiteSpace(detalhes.Idiomas)) proficiencias.Add("Idiomas: " + detalhes.Idiomas.Trim());
                var armaduras = new[]
                {
                    detalhes.ProficienciaArmaduraLeve ? "leve" : null, detalhes.ProficienciaArmaduraMedia ? "média" : null,
                    detalhes.ProficienciaArmaduraPesada ? "pesada" : null, detalhes.ProficienciaEscudos ? "escudos" : null,
                }.Where(x => x != null);
                if (armaduras.Any()) proficiencias.Add("Armaduras: " + string.Join(", ", armaduras));
                if (!string.IsNullOrWhiteSpace(detalhes.ProficienciaArmas)) proficiencias.Add("Armas: " + detalhes.ProficienciaArmas.Trim());
                if (!string.IsNullOrWhiteSpace(detalhes.ProficienciaFerramentas)) proficiencias.Add("Ferramentas: " + detalhes.ProficienciaFerramentas.Trim());
                Definir(t, "ProficienciesLang", string.Join("\n", proficiencias));

                // ---- Página 2 ----
                var hist = ExtrairSecao(historia, "História:");
                if (string.IsNullOrWhiteSpace(hist) && !TemSecoes(historia)) hist = historia;
                var backstory = new List<string>();
                if (!string.IsNullOrWhiteSpace(hist)) backstory.Add(hist.Trim());
                if (!string.IsNullOrWhiteSpace(detalhes.Aparencia)) backstory.Add("Aparência: " + detalhes.Aparencia.Trim());
                Definir(t, "Backstory", string.Join("\n\n", backstory));
                Definir(t, "Feat+Traits", talentos);
            }

            // ---- Página 3: conjuração ----
            var conj = p.PersonagemConjuracao;
            if (conj != null && !string.IsNullOrWhiteSpace(conj.AtributoConjuracao) && !EhNenhum(conj.AtributoConjuracao))
            {
                var codigo = Normalizar(conj.AtributoConjuracao);
                var mod = PontuacaoDe(atributo, codigo) is int v ? RegrasDnd.Modificador(v) : 0;

                Definir(t, "Spellcasting Class 2", jogador?.Classe);
                Definir(t, "SpellcastingAbility 2", conj.AtributoConjuracao);
                Definir(t, "SpellSaveDC 2", (8 + bonusProf + mod).ToString(CultureInfo.InvariantCulture));
                Definir(t, "SpellAtkBonus 2", RegrasDnd.ComSinal(bonusProf + mod));
            }

            if (conj != null)
            {
                var maximos = new[] { conj.SlotsNivel1Max, conj.SlotsNivel2Max, conj.SlotsNivel3Max, conj.SlotsNivel4Max, conj.SlotsNivel5Max, conj.SlotsNivel6Max, conj.SlotsNivel7Max, conj.SlotsNivel8Max, conj.SlotsNivel9Max };
                var atuais = new[] { conj.SlotsNivel1Atual, conj.SlotsNivel2Atual, conj.SlotsNivel3Atual, conj.SlotsNivel4Atual, conj.SlotsNivel5Atual, conj.SlotsNivel6Atual, conj.SlotsNivel7Atual, conj.SlotsNivel8Atual, conj.SlotsNivel9Atual };

                for (var nivel = 1; nivel <= 9; nivel++)
                {
                    var max = maximos[nivel - 1];
                    if (max <= 0) continue;

                    // Na ficha o campo "SlotsRemaining" é, na verdade, a coluna "gastos".
                    Definir(t, $"SlotsTotal {18 + nivel}", max.ToString(CultureInfo.InvariantCulture));
                    Definir(t, $"SlotsRemaining {18 + nivel}", Math.Max(0, max - atuais[nivel - 1]).ToString(CultureInfo.InvariantCulture));
                }
            }

            foreach (var grupo in (p.PersonagemMagias ?? new List<PersonagemMagia>()).GroupBy(m => Math.Clamp(m.Nivel, 0, 9)))
            {
                var linhas = LinhasDeMagia[grupo.Key];
                var nomes = grupo.Select(m => m.Nome).Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();

                for (var i = 0; i < nomes.Count && i < linhas.Length; i++)
                    Definir(t, linhas[i], nomes[i]);

                // Mais magias do que linhas: a última linha avisa quantas ficaram de fora, em vez de sumirem sem aviso.
                if (nomes.Count > linhas.Length)
                    Definir(t, linhas[^1], $"{nomes[linhas.Length - 1]} (+{nomes.Count - linhas.Length} magias)");
            }

            return new FichaPreenchida(t, c);
        }

        /// <summary>Duas grafias de perícia (ex.: "Lidar com Animais" e "Adestrar Animais") são a mesma?</summary>
        public static bool MesmaPericia(string? a, string? b)
        {
            var na = Normalizar(a); var nb = Normalizar(b);
            if (na == "" || nb == "") return false;
            if (na == nb) return true;
            return Pericias.Any(x => x.Nomes.Contains(na) && x.Nomes.Contains(nb));
        }
        // ---------- ataque: mesma conta mostrada na ficha da tela ----------
        private static (string Bonus, string Dano) CalcularAtaque(PersonagemAtaque a, Atributo? atributo, int bonusProf)
        {
            var mod = 0;
            if (!string.IsNullOrWhiteSpace(a.AtributoBase) && PontuacaoDe(atributo, Normalizar(a.AtributoBase)) is int pontuacao)
                mod = RegrasDnd.Modificador(pontuacao);

            var acerto = mod + (a.Proficiente ? bonusProf : 0) + a.BonusAcerto;
            var dano = a.Dano ?? "";
            if (mod != 0) dano += RegrasDnd.ComSinal(mod);

            return (RegrasDnd.ComSinal(acerto), dano);
        }

        // ---------- atributos por código (aceita FOR/STR, DES/DEX, CON, INT, SAB/WIS, CAR/CHA e nomes por extenso) ----------
        private static int? PontuacaoDe(Atributo? a, string codigo)
        {
            if (a == null) return null;
            var k = Normalizar(codigo);

            if (k.StartsWith("for") || k.StartsWith("str")) return a.Forca;
            if (k.StartsWith("des") || k.StartsWith("dex")) return a.Destreza;
            if (k.StartsWith("con")) return a.Constituicao;
            if (k.StartsWith("int")) return a.Inteligencia;
            if (k.StartsWith("sab") || k.StartsWith("wis")) return a.Sabedoria;
            if (k.StartsWith("car") || k.StartsWith("cha")) return a.Carisma;
            return null;
        }

        private static bool SalvaguardaProficiente(Atributo a, string codigo) => codigo switch
        {
            "for" => a.ProficienciaSalvaguardaForca,
            "des" => a.ProficienciaSalvaguardaDestreza,
            "con" => a.ProficienciaSalvaguardaConstituicao,
            "int" => a.ProficienciaSalvaguardaInteligencia,
            "sab" => a.ProficienciaSalvaguardaSabedoria,
            "car" => a.ProficienciaSalvaguardaCarisma,
            _ => false,
        };

        // ---------- utilitários de texto ----------
        private static void Definir(Dictionary<string, string> destino, string campo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor)) destino[campo] = valor.Trim();
        }

        internal static string Primeiro(string? a, string? b) => !string.IsNullOrWhiteSpace(a) ? a! : (b ?? "");

        private static bool EhNenhum(string valor) => Normalizar(valor) == "nenhum";

        private static bool TemSecoes(string texto) =>
            texto.Split('\n').Any(l => l.StartsWith("Traços de Espécie:") || l.StartsWith("Talentos:") || l.StartsWith("História:"));

        // O importador grava as seções no texto como "Título: conteúdo", uma por linha.
        private static string ExtrairSecao(string texto, string titulo)
        {
            foreach (var linha in texto.Split('\n'))
                if (linha.StartsWith(titulo)) return linha.Substring(titulo.Length).Trim();
            return "";
        }

        /// <summary>Minúsculas e sem acentos, para comparar nomes ("Atuação" == "atuacao").</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var sb = new StringBuilder();
            foreach (var ch in texto.Trim().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
