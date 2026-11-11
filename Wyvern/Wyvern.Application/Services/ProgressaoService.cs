using System.Text.RegularExpressions;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Subida de nível de personagens de jogador pelas regras do D&D 2024. A prévia diz o que muda; a aplicação
    /// confere as escolhas do jogador e atualiza a ficha (PV, proficiência, dados de vida, espaços de magia, atributos...).
    /// </summary>
    public static class ProgressaoService
    {
        public static readonly string[] ChavesAtributo = { "forca", "destreza", "constituicao", "inteligencia", "sabedoria", "carisma" };
        private static readonly Regex Dado = new(@"d\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int Ler(Atributo? a, string chave) => a == null ? 10 : chave switch
        {
            "forca" => a.Forca, "destreza" => a.Destreza, "constituicao" => a.Constituicao,
            "inteligencia" => a.Inteligencia, "sabedoria" => a.Sabedoria, "carisma" => a.Carisma,
            _ => throw new RegraNegocioException($"Atributo desconhecido: {chave}."),
        };

        private static void Escrever(Atributo a, string chave, int valor)
        {
            switch (chave)
            {
                case "forca": a.Forca = valor; break;
                case "destreza": a.Destreza = valor; break;
                case "constituicao": a.Constituicao = valor; break;
                case "inteligencia": a.Inteligencia = valor; break;
                case "sabedoria": a.Sabedoria = valor; break;
                case "carisma": a.Carisma = valor; break;
            }
        }

        private static int[] EspacosDaFicha(PersonagemConjuracao? c) => c == null ? new int[9] : new[]
        {
            c.SlotsNivel1Max, c.SlotsNivel2Max, c.SlotsNivel3Max, c.SlotsNivel4Max, c.SlotsNivel5Max,
            c.SlotsNivel6Max, c.SlotsNivel7Max, c.SlotsNivel8Max, c.SlotsNivel9Max,
        };

        /// <summary>Dado de vida: o da classe; para classes fora do livro, o que a ficha diz ("5d10"); senão d8.</summary>
        private static int DadoVida(ClasseDnd? classe, PersonagemCombate? combate)
        {
            if (classe != null) return classe.DadoVida;
            var m = Dado.Match(combate?.DadoVidaMaximo ?? string.Empty);
            return m.Success && int.TryParse(m.Groups[1].Value, out var d) && d is >= 4 and <= 20 ? d : 8;
        }

        public static ProximoNivelDto Previa(Personagem p)
        {
            var player = p.PersonagemPlayer;
            var combate = p.PersonagemCombate;
            var classe = RegrasDnd2024.AcharClasse(player?.Classe);
            var nivel = Math.Max(1, player?.Nivel ?? 1);
            var proximo = Math.Min(RegrasDnd2024.NivelMaximo, nivel + 1);
            var xp = player?.Xp ?? 0;
            var liberado = player?.NivelLiberado ?? 0;
            var dado = DadoVida(classe, combate);

            var previa = new ProximoNivelDto
            {
                ClasseReconhecida = classe != null,
                Classe = classe?.Nome ?? player?.Classe ?? string.Empty,
                NivelAtual = nivel,
                ProximoNivel = proximo,
                NivelDisponivel = RegrasDnd2024.NivelDisponivel(xp, liberado),
                Xp = xp,
                XpProximoNivel = nivel < RegrasDnd2024.NivelMaximo ? RegrasDnd2024.XpPorNivel[nivel] : null,
                NivelLiberado = liberado,
                DadoVida = dado,
                PvFixo = RegrasDnd2024.PvFixo(dado),
                ModConstituicao = RegrasDnd2024.Modificador(Ler(p.Atributo, "constituicao")),
                VidaMaxima = combate?.VidaMaxima ?? 0,
                Proficiencia = new ValorComparadoDto { Antes = combate?.ProficienciaBonus ?? RegrasDnd2024.Proficiencia(nivel), Depois = RegrasDnd2024.Proficiencia(proximo) },
                Atributos = ChavesAtributo.ToDictionary(c => c, c => Ler(p.Atributo, c)),
            };

            if (p.TipoId != 1 || player == null) previa.Motivo = "Só personagens de jogador sobem de nível.";
            else if (nivel >= RegrasDnd2024.NivelMaximo) previa.Motivo = "O personagem já está no nível máximo (20).";
            else if (combate == null) previa.Motivo = "A ficha ainda não tem pontos de vida. Preencha a parte de combate antes.";
            else if (previa.NivelDisponivel < proximo) previa.Motivo = "O Mestre ainda não liberou o próximo nível.";
            previa.PodeSubir = previa.Motivo == null;

            if (classe != null && nivel < RegrasDnd2024.NivelMaximo)
            {
                previa.Caracteristicas = RegrasDnd2024.CaracteristicasDoNivel(classe, proximo);
                previa.Recursos = classe.Recursos
                    .Select(r => new RecursoComparadoDto { Nome = r.Nome, Antes = r.Valor(nivel), Depois = r.Valor(proximo) })
                    .Where(r => r.Depois != null)
                    .ToList();
                if (classe.Conjurador != TipoConjurador.Nenhum)
                {
                    previa.EspacosAntes = EspacosDaFicha(p.PersonagemConjuracao);
                    previa.EspacosDepois = RegrasDnd2024.Espacos(classe.Conjurador, proximo);
                }
                if (classe.Truques != null) previa.Truques = new ValorComparadoDto { Antes = classe.Truques[nivel - 1], Depois = classe.Truques[proximo - 1] };
                if (classe.MagiasPreparadas != null) previa.MagiasPreparadas = new ValorComparadoDto { Antes = classe.MagiasPreparadas[nivel - 1], Depois = classe.MagiasPreparadas[proximo - 1] };
                previa.EscolheSubclasse = proximo >= RegrasDnd2024.NivelSubclasse && string.IsNullOrWhiteSpace(player?.Subclasse);
                previa.MelhoriaDeAtributo = RegrasDnd2024.TemMelhoria(classe, proximo);
                previa.DadivaEpica = proximo == RegrasDnd2024.NivelDadivaEpica;
            }

            return previa;
        }

        /// <summary>Aplica a subida de nível. Devolve os PV ganhos e um resumo para a crônica.</summary>
        public static (int PvGanhos, string Resumo) Aplicar(Personagem p, SubirNivelDto dto)
        {
            var previa = Previa(p);
            if (!previa.PodeSubir) throw new RegraNegocioException(previa.Motivo!);

            var player = p.PersonagemPlayer!;
            var combate = p.PersonagemCombate!;
            var proximo = previa.ProximoNivel;

            // ---- PV ----
            int basePv;
            if (string.Equals(dto.ModoPv, "Fixo", StringComparison.OrdinalIgnoreCase)) basePv = previa.PvFixo;
            else if (string.Equals(dto.ModoPv, "Rolado", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.PvRolado is not { } rolado || rolado < 1 || rolado > previa.DadoVida)
                    throw new RegraNegocioException($"O resultado do dado de vida deve ficar entre 1 e {previa.DadoVida}.");
                basePv = rolado;
            }
            else throw new RegraNegocioException("Escolha como ganhar PV: \"Fixo\" ou \"Rolado\".");

            // ---- subclasse ----
            var subclasse = dto.Subclasse?.Trim();
            if (previa.EscolheSubclasse && proximo == RegrasDnd2024.NivelSubclasse && string.IsNullOrEmpty(subclasse))
                throw new RegraNegocioException("No nível 3 o personagem escolhe a subclasse.");
            if (subclasse?.Length > 50) throw new RegraNegocioException("O nome da subclasse pode ter até 50 caracteres.");

            // ---- melhoria de atributo / talento / dádiva épica ----
            var aumentos = new Dictionary<string, int>();
            string? talento = null;
            if (previa.MelhoriaDeAtributo || previa.DadivaEpica)
            {
                var m = dto.Melhoria ?? throw new RegraNegocioException(previa.DadivaEpica
                    ? "No nível 19 o personagem escolhe uma Dádiva Épica (ou outro talento)."
                    : "Neste nível o personagem escolhe a Melhoria de Atributo ou outro talento.");

                foreach (var (chave, valor) in m.Aumentos.Where(a => a.Value != 0))
                {
                    var c = chave.ToLowerInvariant();
                    if (!ChavesAtributo.Contains(c)) throw new RegraNegocioException($"Atributo desconhecido: {chave}.");
                    if (valor < 0) throw new RegraNegocioException("Os aumentos de atributo não podem ser negativos.");
                    aumentos[c] = valor;
                }
                var soma = aumentos.Values.Sum();

                if (string.Equals(m.Tipo, "Atributos", StringComparison.OrdinalIgnoreCase))
                {
                    if (soma != 2 || aumentos.Values.Any(v => v > 2))
                        throw new RegraNegocioException("Aprimoramento de Atributo: +2 em um atributo ou +1 em dois.");
                    talento = "Aprimoramento de Atributo";
                }
                else if (string.Equals(m.Tipo, "Talento", StringComparison.OrdinalIgnoreCase))
                {
                    talento = m.Talento?.Trim();
                    if (string.IsNullOrEmpty(talento)) throw new RegraNegocioException("Diga qual talento o personagem escolheu.");
                    if (talento.Length > 80) throw new RegraNegocioException("O nome do talento pode ter até 80 caracteres.");
                    if (soma > 1) throw new RegraNegocioException("Um talento dá no máximo +1 em um atributo.");
                }
                else throw new RegraNegocioException("Escolha \"Atributos\" ou \"Talento\".");

                // Dádivas Épicas sobem até 30; o resto até 20
                var teto = previa.DadivaEpica && !string.Equals(m.Tipo, "Atributos", StringComparison.OrdinalIgnoreCase) ? 30 : 20;
                foreach (var (c, v) in aumentos)
                    if (Ler(p.Atributo, c) + v > teto) throw new RegraNegocioException($"Nenhum atributo passa de {teto} com essa escolha.");
                if (aumentos.Count > 0 && p.Atributo == null) throw new RegraNegocioException("A ficha ainda não tem atributos.");
            }

            // ---- aplica ----
            var conAntes = RegrasDnd2024.Modificador(Ler(p.Atributo, "constituicao"));
            foreach (var (c, v) in aumentos) Escrever(p.Atributo!, c, Ler(p.Atributo, c) + v);
            var conDepois = RegrasDnd2024.Modificador(Ler(p.Atributo, "constituicao"));

            // PV do nível novo com a CON antiga (mínimo 1), mais o ajuste retroativo se a CON subiu: +1 por nível
            var pvGanhos = Math.Max(1, basePv + conAntes) + (conDepois - conAntes) * proximo;
            combate.VidaMaxima += pvGanhos;
            combate.VidaAtual = Math.Clamp(combate.VidaAtual + pvGanhos, 0, combate.VidaMaxima);
            combate.ProficienciaBonus = previa.Proficiencia.Depois;
            combate.DadoVidaMaximo = $"{proximo}d{previa.DadoVida}";

            player.Nivel = proximo;
            if (!string.IsNullOrEmpty(subclasse) && string.IsNullOrWhiteSpace(player.Subclasse)) player.Subclasse = subclasse;

            if (previa.EspacosDepois != null)
            {
                var c = p.PersonagemConjuracao ??= new PersonagemConjuracao { PersonagemId = p.PersonagemId };
                var antes = EspacosDaFicha(c);
                for (var i = 0; i < 9; i++)
                {
                    var novo = previa.EspacosDepois[i];
                    var ganho = Math.Max(0, novo - antes[i]);
                    DefinirEspaco(c, i + 1, novo, Math.Min(novo, LerAtual(c, i + 1) + ganho));
                }
            }

            // Fica registrado na ficha o que o nível trouxe
            var d = p.PersonagemDetalhes ??= new PersonagemDetalhes { PersonagemId = p.PersonagemId };
            if (previa.Caracteristicas.Count > 0)
                d.CaracteristicasClasse = Acrescentar(d.CaracteristicasClasse, $"Nível {proximo}: {string.Join(", ", previa.Caracteristicas)}");
            if (talento != null)
            {
                var detalhe = aumentos.Count > 0 ? $" ({string.Join(", ", aumentos.Select(a => $"{NomeAtributo(a.Key)} +{a.Value}"))})" : string.Empty;
                d.Talentos = Acrescentar(d.Talentos, $"{talento}{detalhe} (nível {proximo})");
            }

            var partes = new List<string> { $"+{pvGanhos} PV" };
            if (previa.Proficiencia.Depois != previa.Proficiencia.Antes) partes.Add($"proficiência +{previa.Proficiencia.Depois}");
            if (!string.IsNullOrEmpty(subclasse)) partes.Add($"subclasse {subclasse}");
            if (talento != null) partes.Add(talento);
            partes.AddRange(previa.Caracteristicas.Where(c => c != RegrasDnd2024.Melhoria && c != RegrasDnd2024.DadivaEpica));
            return (pvGanhos, $"{p.Nome} chegou ao nível {proximo}: {string.Join(", ", partes)}.");
        }

        private static string Acrescentar(string? texto, string linha) =>
            string.IsNullOrWhiteSpace(texto) ? linha : $"{texto.TrimEnd()}\n{linha}";

        public static string NomeAtributo(string chave) => chave switch
        {
            "forca" => "FOR", "destreza" => "DES", "constituicao" => "CON",
            "inteligencia" => "INT", "sabedoria" => "SAB", "carisma" => "CAR", _ => chave,
        };

        private static int LerAtual(PersonagemConjuracao c, int n) => n switch
        {
            1 => c.SlotsNivel1Atual, 2 => c.SlotsNivel2Atual, 3 => c.SlotsNivel3Atual, 4 => c.SlotsNivel4Atual, 5 => c.SlotsNivel5Atual,
            6 => c.SlotsNivel6Atual, 7 => c.SlotsNivel7Atual, 8 => c.SlotsNivel8Atual, _ => c.SlotsNivel9Atual,
        };

        private static void DefinirEspaco(PersonagemConjuracao c, int n, int max, int atual)
        {
            switch (n)
            {
                case 1: c.SlotsNivel1Max = max; c.SlotsNivel1Atual = atual; break;
                case 2: c.SlotsNivel2Max = max; c.SlotsNivel2Atual = atual; break;
                case 3: c.SlotsNivel3Max = max; c.SlotsNivel3Atual = atual; break;
                case 4: c.SlotsNivel4Max = max; c.SlotsNivel4Atual = atual; break;
                case 5: c.SlotsNivel5Max = max; c.SlotsNivel5Atual = atual; break;
                case 6: c.SlotsNivel6Max = max; c.SlotsNivel6Atual = atual; break;
                case 7: c.SlotsNivel7Max = max; c.SlotsNivel7Atual = atual; break;
                case 8: c.SlotsNivel8Max = max; c.SlotsNivel8Atual = atual; break;
                default: c.SlotsNivel9Max = max; c.SlotsNivel9Atual = atual; break;
            }
        }

        /// <summary>
        /// Na edição comum da ficha, quem não é Mestre não mexe no que só muda subindo de nível:
        /// nível, XP, classe e subclasse, PV máximo, proficiência, dados de vida, atributos, salvaguardas e espaços de magia máximos.
        /// Copia esses valores do banco para o pedido antes de aplicá-lo.
        /// </summary>
        public static void ProtegerCamposDeNivel(Personagem banco, PersonagemUpdateDto dto)
        {
            if (dto.PersonagemPlayer != null && banco.PersonagemPlayer is { } pl)
            {
                dto.PersonagemPlayer.Nivel = pl.Nivel;
                dto.PersonagemPlayer.Xp = pl.Xp;
                dto.PersonagemPlayer.Classe = pl.Classe;
                dto.PersonagemPlayer.Subclasse = pl.Subclasse;
            }
            if (dto.PersonagemCombate != null && banco.PersonagemCombate is { } cb)
            {
                dto.PersonagemCombate.VidaMaxima = cb.VidaMaxima;
                dto.PersonagemCombate.ProficienciaBonus = cb.ProficienciaBonus;
                dto.PersonagemCombate.DadoVidaMaximo = cb.DadoVidaMaximo;
                // os PV atuais continuam editáveis, mas nunca acima do máximo
                dto.PersonagemCombate.VidaAtual = Math.Min(dto.PersonagemCombate.VidaAtual, cb.VidaMaxima);
            }
            if (banco.Atributo != null) dto.Atributo = null; // atributos e salvaguardas ficam como estão
            if (dto.PersonagemConjuracao != null && banco.PersonagemConjuracao is { } cj)
            {
                dto.PersonagemConjuracao.SlotsNivel1Max = cj.SlotsNivel1Max; dto.PersonagemConjuracao.SlotsNivel2Max = cj.SlotsNivel2Max;
                dto.PersonagemConjuracao.SlotsNivel3Max = cj.SlotsNivel3Max; dto.PersonagemConjuracao.SlotsNivel4Max = cj.SlotsNivel4Max;
                dto.PersonagemConjuracao.SlotsNivel5Max = cj.SlotsNivel5Max; dto.PersonagemConjuracao.SlotsNivel6Max = cj.SlotsNivel6Max;
                dto.PersonagemConjuracao.SlotsNivel7Max = cj.SlotsNivel7Max; dto.PersonagemConjuracao.SlotsNivel8Max = cj.SlotsNivel8Max;
                dto.PersonagemConjuracao.SlotsNivel9Max = cj.SlotsNivel9Max;
            }
        }
    }
}
