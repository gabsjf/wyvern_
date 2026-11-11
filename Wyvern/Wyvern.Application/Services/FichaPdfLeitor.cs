using System.Globalization;
using System.Text.RegularExpressions;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Faz o caminho inverso do <see cref="FichaPdfMapper"/>: dos campos da ficha padrão de D&amp;D 5e para um personagem.
    /// Também é uma função pura (campo → valor), então dá para testar sem abrir PDF.
    /// Perícias saem com o nome em português e <c>PericiaId = 0</c>; quem grava resolve o id no cadastro.
    /// </summary>
    public static class FichaPdfLeitor
    {
        private static readonly Regex NivelNoFinal = new(@"^(?<classe>.*?)\s*(?<nivel>\d{1,2})$", RegexOptions.Compiled);
        private static readonly Regex Numeros = new(@"\d+", RegexOptions.Compiled);
        private static readonly Regex Subclasse = new(@"^(?<classe>[^()]*?)\s*\((?<sub>[^)]+)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex Dano = new(@"^\s*(?<dados>\d+d\d+)\s*(?<mod>[+-]\s*\d+)?\s*(?<tipo>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MaisMagias = new(@"\s*\(\+\d+ magias\)\s*$", RegexOptions.Compiled);
        private static readonly Regex AtaqueEmTexto = new(@"^(?<nome>.+?)\s+(?<bonus>[+-]\s*\d+)\s*,\s*(?<dano>.+)$", RegexOptions.Compiled);
        private static readonly Regex QuantidadeDeDados = new(@"^\s*(?<n>\d+)", RegexOptions.Compiled);
        private static readonly Regex Raridade = new(@"^(?<nome>.*?)\s*\((?<raridade>Comum|Incomum|Raro|Muito Raro|Lendário|Lendario|Artefato)\)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] NomesAtributo = { "Forca", "Destreza", "Constituicao", "Inteligencia", "Sabedoria", "Carisma" };

        public static Personagem Ler(IReadOnlyDictionary<string, string> textos, IReadOnlyDictionary<string, bool> caixas, int campanhaId, int criadoPorId)
        {
            string T(string nome) => textos.TryGetValue(nome, out var v) ? v.Trim() : "";
            bool C(string nome) => caixas.TryGetValue(nome, out var v) && v;
            int N(string nome) => Inteiro(T(nome));

            var nome = T("CharacterName");
            if (nome == "") nome = T("CharacterName 2");
            if (nome == "") nome = "Personagem Desconhecido";

            var (classe, subclasse, nivel) = LerClasse(T("ClassLevel"));
            var xp = N("XP");

            var p = new Personagem
            {
                Nome = nome,
                Descricao = "Importado do PDF 5e Padrão",
                CriadoEm = DateTime.UtcNow,
                Ativo = true,
                CampanhaId = campanhaId,
                CriadoPorId = criadoPorId,
                TipoId = 1,
                PersonagemPlayer = new PersonagemPlayer
                {
                    Classe = classe, Subclasse = subclasse, Nivel = nivel, Xp = xp,
                    Raca = T("Race"), Antecedente = T("Background"),
                    Alinhamento = T("Alignment") == "" ? "Desconhecido" : T("Alignment"),
                    Tamanho = "Médio",
                },
                PersonagemDinheiro = new PersonagemDinheiro { PC = N("CP"), PP = N("SP"), PE = N("EP"), PO = N("GP"), PL = N("PP") },
            };

            // ---- Atributos e salvaguardas ----
            // Cada atributo tem dois campos (quadrado grande e oval); quem preencheu pode ter usado qualquer um para a pontuação.
            int Atr(string codigo)
            {
                var (_, quadrado, oval) = FichaPdfMapper.CamposAtributos.First(x => x.Atributo == codigo);
                return EscolherPontuacao(T(quadrado), T(oval));
            }
            var a = new Atributo
            {
                Forca = Atr("for"), Destreza = Atr("des"), Constituicao = Atr("con"),
                Inteligencia = Atr("int"), Sabedoria = Atr("sab"), Carisma = Atr("car"),
                ProficienciaSalvaguardaForca = C("Check Box 11"), ProficienciaSalvaguardaDestreza = C("Check Box 18"),
                ProficienciaSalvaguardaConstituicao = C("Check Box 19"), ProficienciaSalvaguardaInteligencia = C("Check Box 20"),
                ProficienciaSalvaguardaSabedoria = C("Check Box 21"), ProficienciaSalvaguardaCarisma = C("Check Box 22"),
            };
            p.Atributo = a;
            int[] pontuacoes = { a.Forca, a.Destreza, a.Constituicao, a.Inteligencia, a.Sabedoria, a.Carisma };

            // ---- Combate ----
            var bonusProf = textos.ContainsKey("ProfBonus") ? Inteiro(T("ProfBonus")) : 2 + (nivel - 1) / 4;
            if (bonusProf <= 0) bonusProf = 2 + (nivel - 1) / 4;

            var vidaMax = N("HPMax");
            var vidaAtual = T("HPCurrent") == "" ? vidaMax : N("HPCurrent");
            var iniciativa = Inteiro(T("Initiative"));
            var modDes = Wyvern.Domain.Regras.RegrasDnd.Modificador(a.Destreza);

            p.PersonagemCombate = new PersonagemCombate
            {
                ClasseArmadura = N("AC"),
                // O mapeador grava o modificador de Destreza quando a iniciativa não foi cadastrada: volta a ser "não cadastrada".
                Iniciativa = iniciativa == modDes ? 0 : iniciativa,
                Deslocamento = T("Speed"),
                VidaMaxima = vidaMax, VidaAtual = vidaAtual, VidaTemporaria = N("HPTemp"),
                DadoVidaMaximo = T("HDTotal") == "" ? null : T("HDTotal"),
                DadoVidaGasto = DadosDeVidaGastos(T("HDTotal"), T("HD"), nivel),
                InspiracaoHeroica = T("Inspiration") != "" && T("Inspiration") != "0",
                ProficienciaBonus = bonusProf,
                DeathSaveSucessos = Enumerable.Range(12, 3).Count(i => C($"Check Box {i}")),
                DeathSaveFalhas = Enumerable.Range(15, 3).Count(i => C($"Check Box {i}")),
            };

            // ---- Perícias: marcada = proficiente; o que sobra no total vira bônus extra ----
            p.PersonagemPericias = new List<PersonagemPericia>();
            foreach (var pericia in FichaPdfMapper.Pericias)
            {
                if (!C(pericia.Caixa)) continue;

                var indice = Array.FindIndex(FichaPdfMapper.CamposAtributos, x => x.Atributo == pericia.Atributo);
                var esperado = Wyvern.Domain.Regras.RegrasDnd.Modificador(pontuacoes[indice]) + bonusProf;
                var bonus = T(pericia.Campo) == "" ? 0 : Inteiro(T(pericia.Campo)) - esperado;

                p.PersonagemPericias.Add(new PersonagemPericia
                {
                    Bonus = bonus,
                    Proficiente = true,
                    Pericia = new Pericia { Nome = NomeCapitalizado(pericia.Nomes[0]) },
                });
            }

            // ---- Ataques (3 linhas da ficha) ----
            p.PersonagemAtaques = new List<PersonagemAtaque>();
            var nomes = new[] { "Wpn Name", "Wpn Name 2", "Wpn Name 3" };
            var bonusCampos = new[] { "Wpn1 AtkBonus", "Wpn2 AtkBonus", "Wpn3 AtkBonus" };
            var danoCampos = new[] { "Wpn1 Damage", "Wpn2 Damage", "Wpn3 Damage" };
            for (var i = 0; i < 3; i++)
            {
                if (T(nomes[i]) == "") continue;
                p.PersonagemAtaques.Add(LerAtaque(T(nomes[i]), T(bonusCampos[i]), T(danoCampos[i]), pontuacoes, bonusProf));
            }

            // Do quarto ataque em diante a exportação escreve no quadro de texto, um por linha: "Adaga +5, 1d4+3 perfurante".
            foreach (var linha in T("AttacksSpellcasting").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var m = AtaqueEmTexto.Match(linha.Trim());
                if (m.Success)
                    p.PersonagemAtaques.Add(LerAtaque(m.Groups["nome"].Value.Trim(), m.Groups["bonus"].Value, m.Groups["dano"].Value, pontuacoes, bonusProf));
            }

            // ---- Equipamento: uma linha por item ----
            p.PersonagemItens = new List<PersonagemItem>();
            foreach (var linha in T("Equipment").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var texto = linha.Trim().TrimStart('•', '-', '*', ' ').Trim();
                if (texto == "") continue;

                var rar = Raridade.Match(texto);
                p.PersonagemItens.Add(new PersonagemItem
                {
                    Nome = rar.Success ? rar.Groups["nome"].Value : texto,
                    TipoItem = "Equipamento",
                    Raridade = rar.Success ? rar.Groups["raridade"].Value : "Comum",
                    Descricao = "",
                });
            }

            p.PersonagemDetalhes = LerDetalhes(T, p.PersonagemPlayer);
            p.PersonagemConjuracao = LerConjuracao(T, N);
            p.PersonagemMagias = LerMagias(T);

            return p;
        }

        // "Bardo (Colégio da Bravura) 5" → Bardo / Colégio da Bravura / 5.  "Guerreiro 3 / Mago 2" → nível 5 (soma).
        public static (string Classe, string? Subclasse, int Nivel) LerClasse(string texto)
        {
            texto = texto.Trim();
            if (texto == "") return ("", null, 1);

            var numeros = Numeros.Matches(texto);
            if (numeros.Count > 1)
                return (texto, null, Math.Clamp(numeros.Sum(m => int.Parse(m.Value, CultureInfo.InvariantCulture)), 1, 20));

            var m = NivelNoFinal.Match(texto);
            var nivel = 1;
            var resto = texto;
            if (m.Success)
            {
                nivel = Math.Clamp(int.Parse(m.Groups["nivel"].Value, CultureInfo.InvariantCulture), 1, 20);
                resto = m.Groups["classe"].Value.Trim();
            }

            var s = Subclasse.Match(resto);
            return s.Success ? (s.Groups["classe"].Value.Trim(), s.Groups["sub"].Value.Trim(), nivel) : (resto, null, nivel);
        }

        private static PersonagemAtaque LerAtaque(string nome, string bonusTexto, string danoTexto, int[] pontuacoes, int bonusProf)
        {
            var ataque = new PersonagemAtaque { Nome = nome, Alcance = "", Propriedades = "", Dano = danoTexto, TipoDano = "", Proficiente = true };

            var bonus = Inteiro(bonusTexto);
            var m = Dano.Match(danoTexto);
            var modDano = 0;
            if (m.Success)
            {
                ataque.Dano = m.Groups["dados"].Value;
                ataque.TipoDano = m.Groups["tipo"].Value.Trim();
                if (m.Groups["mod"].Success) modDano = Inteiro(m.Groups["mod"].Value.Replace(" ", ""));
            }

            // O mapeador escreve "dados + modificador do atributo"; descobre o atributo cujo modificador bate (Força/Destreza primeiro).
            var ordem = new[] { 0, 1, 2, 3, 4, 5 };
            var escolhido = modDano == 0 ? -1 : ordem.FirstOrDefault(i => Wyvern.Domain.Regras.RegrasDnd.Modificador(pontuacoes[i]) == modDano, -1);
            ataque.AtributoBase = escolhido >= 0 ? NomesAtributo[escolhido] : "Forca";
            var mod = escolhido >= 0 ? modDano : 0;

            // acerto = mod + (proficiente ? prof : 0) + bônus: sem proficiência se o total não comporta o bônus de proficiência.
            ataque.Proficiente = bonus - mod >= bonusProf;
            ataque.BonusAcerto = bonus - mod - (ataque.Proficiente ? bonusProf : 0);
            return ataque;
        }

        private static PersonagemDetalhes LerDetalhes(Func<string, string> T, PersonagemPlayer jogador)
        {
            var d = new PersonagemDetalhes { Aparencia = null };

            // "Features and Traits" = características + "Traços de espécie:\n..." + "Talentos:\n...", separados por linha em branco.
            var blocos = T("Features and Traits").Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var caracteristicas = new List<string>();
            foreach (var bloco in blocos)
            {
                if (bloco.StartsWith("Traços de espécie:")) d.TracosEspecie = bloco["Traços de espécie:".Length..].Trim();
                else if (bloco.StartsWith("Talentos:")) d.Talentos = bloco["Talentos:".Length..].Trim();
                else caracteristicas.Add(bloco.Trim());
            }
            d.CaracteristicasClasse = string.Join("\n\n", caracteristicas);
            // Os talentos ficam na página 2; fichas exportadas antes desta versão os traziam num bloco "Talentos:" da página 1.
            if (T("Feat+Traits") != "") d.Talentos = T("Feat+Traits");

            // Proficiências: linhas "Idiomas: ...", "Armaduras: leve, média", "Armas: ...", "Ferramentas: ..."
            foreach (var linha in T("ProficienciesLang").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Depois(linha, "Idiomas:") is { } idiomas) d.Idiomas = idiomas;
                else if (Depois(linha, "Armaduras:") is { } armaduras)
                {
                    var n = FichaPdfMapper.Normalizar(armaduras);
                    d.ProficienciaArmaduraLeve = n.Contains("leve");
                    d.ProficienciaArmaduraMedia = n.Contains("media");
                    d.ProficienciaArmaduraPesada = n.Contains("pesada");
                    d.ProficienciaEscudos = n.Contains("escudo");
                }
                else if (Depois(linha, "Armas:") is { } armas) d.ProficienciaArmas = armas;
                else if (Depois(linha, "Ferramentas:") is { } ferramentas) d.ProficienciaFerramentas = ferramentas;
            }

            // História: texto livre do quadro "Backstory" (a linha "Aparência:" volta para o campo próprio).
            var historia = new List<string>();
            foreach (var bloco in T("Backstory").Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Depois(bloco, "Aparência:") is { } aparencia) d.Aparencia = aparencia;
                else historia.Add(bloco.Trim());
            }

            // Se a ficha tem os quadros de personalidade, guarda no formato "Título: conteúdo" usado no resto do sistema,
            // mantendo as quebras de linha (a exportação lê de volta cada seção para o seu quadro).
            var personalidade = new (string Titulo, string? Valor)[]
            {
                (HistoriaEmSecoes.Tracos, T("PersonalityTraits")), (HistoriaEmSecoes.Ideais, T("Ideals")),
                (HistoriaEmSecoes.Vinculos, T("Bonds")), (HistoriaEmSecoes.Defeitos, T("Flaws")),
            };
            var texto = string.Join("\n\n", historia);
            d.HistoriaPersonalidade = personalidade.Any(x => x.Valor != "")
                ? HistoriaEmSecoes.Montar(null, personalidade.Append((HistoriaEmSecoes.Historia, texto)).ToArray())
                : texto;

            return d;
        }

        private static PersonagemConjuracao LerConjuracao(Func<string, string> T, Func<string, int> N)
        {
            var conj = new PersonagemConjuracao { AtributoConjuracao = T("SpellcastingAbility 2") == "" ? "Nenhum" : T("SpellcastingAbility 2") };

            var maximos = new int[9];
            var gastos = new int[9];
            for (var nivel = 1; nivel <= 9; nivel++)
            {
                maximos[nivel - 1] = N($"SlotsTotal {18 + nivel}");
                gastos[nivel - 1] = N($"SlotsRemaining {18 + nivel}");   // na ficha, esta coluna guarda os gastos
            }

            conj.SlotsNivel1Max = maximos[0]; conj.SlotsNivel2Max = maximos[1]; conj.SlotsNivel3Max = maximos[2];
            conj.SlotsNivel4Max = maximos[3]; conj.SlotsNivel5Max = maximos[4]; conj.SlotsNivel6Max = maximos[5];
            conj.SlotsNivel7Max = maximos[6]; conj.SlotsNivel8Max = maximos[7]; conj.SlotsNivel9Max = maximos[8];

            int Atual(int i) => Math.Max(0, maximos[i] - gastos[i]);
            conj.SlotsNivel1Atual = Atual(0); conj.SlotsNivel2Atual = Atual(1); conj.SlotsNivel3Atual = Atual(2);
            conj.SlotsNivel4Atual = Atual(3); conj.SlotsNivel5Atual = Atual(4); conj.SlotsNivel6Atual = Atual(5);
            conj.SlotsNivel7Atual = Atual(6); conj.SlotsNivel8Atual = Atual(7); conj.SlotsNivel9Atual = Atual(8);
            return conj;
        }

        private static List<PersonagemMagia> LerMagias(Func<string, string> T)
        {
            var magias = new List<PersonagemMagia>();
            foreach (var (nivel, linhas) in FichaPdfMapper.LinhasDeMagia)
            {
                foreach (var campo in linhas)
                {
                    var nome = MaisMagias.Replace(T(campo), "").Trim();
                    if (nome == "") continue;
                    magias.Add(new PersonagemMagia { Nome = nome, Nivel = nivel, Escola = "", Descricao = "" });
                }
            }
            return magias;
        }

        // ---------- utilitários ----------
        private static string? Depois(string linha, string prefixo) =>
            linha.TrimStart().StartsWith(prefixo, StringComparison.OrdinalIgnoreCase) ? linha.TrimStart()[prefixo.Length..].Trim() : null;

        // Aceita "+3", "-1", " 12 ", "12 ft." (pega o primeiro número com sinal opcional); vazio ou lixo = 0.
        public static int Inteiro(string texto)
        {
            var m = Regex.Match(texto ?? "", @"[+-]?\s*\d+");
            return m.Success && int.TryParse(m.Value.Replace(" ", ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        /// <summary>
        /// Pontuação de um atributo a partir dos dois campos da ficha. A ficha oficial pede o modificador no quadrado grande
        /// e a pontuação no oval, mas muita gente (e versões antigas da nossa exportação) faz o contrário. Vale como
        /// pontuação o número sem sinal entre 1 e 30; com os dois assim, o que tem o outro como modificador.
        /// Só com o modificador, reconstrói a pontuação (+3 → 16). Nada preenchido: 10.
        /// </summary>
        public static int EscolherPontuacao(string quadrado, string oval)
        {
            static bool ComSinal(string s) => s.TrimStart().StartsWith('+') || s.TrimStart().StartsWith('-');
            static int? ComoPontuacao(string s)
            {
                if (string.IsNullOrWhiteSpace(s) || ComSinal(s)) return null;
                var v = Inteiro(s);
                return v is >= 1 and <= 30 ? v : null;
            }

            var noOval = ComoPontuacao(oval);
            var noQuadrado = ComoPontuacao(quadrado);
            if (noOval is int o && noQuadrado is int q)
            {
                if (Wyvern.Domain.Regras.RegrasDnd.Modificador(o) == q) return o;
                if (Wyvern.Domain.Regras.RegrasDnd.Modificador(q) == o) return q;
                return Math.Max(o, q); // modificadores vão até +10; na dúvida, o maior é a pontuação
            }
            if (noOval is int soOval) return soOval;
            if (noQuadrado is int soQuadrado) return soQuadrado;

            // só o modificador (com sinal, em qualquer um dos dois): a menor pontuação que dá esse modificador
            var comSinal = new[] { quadrado, oval }.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s) && ComSinal(s));
            return comSinal != null ? Math.Clamp(10 + 2 * Inteiro(comSinal), 1, 30) : 10;
        }

        /// <summary>
        /// Dados de vida gastos = total ("5d8" → 5; sem número, o nível) − os que restam no quadro "Hit Dice".
        /// Quadro vazio: nenhum gasto.
        /// </summary>
        public static int DadosDeVidaGastos(string total, string restantes, int nivel)
        {
            if (string.IsNullOrWhiteSpace(restantes)) return 0;
            var m = QuantidadeDeDados.Match(total ?? "");
            var quantos = m.Success ? int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture) : nivel;
            var sobram = QuantidadeDeDados.Match(restantes);
            if (!sobram.Success) return 0;
            return Math.Clamp(quantos - int.Parse(sobram.Groups["n"].Value, CultureInfo.InvariantCulture), 0, quantos);
        }

        private static string NomeCapitalizado(string normalizado)
        {
            // Nomes do mapeador estão sem acento ("atuacao"); o cadastro é casado por MesmaPericia, que ignora acento e caixa.
            return CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(normalizado);
        }
    }
}
