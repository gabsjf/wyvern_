using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Cena
{
    /// <summary>
    /// O que os jogadores enxergam do mapa, decidido no servidor: com a névoa ligada, um token só sai para eles se está
    /// num quadrado revelado ou ao alcance da visão de algum token (sem parede no caminho). Mesma conta do mapa no
    /// navegador (utils/visao.ts), para a tela e o servidor concordarem; a diferença é que aqui o que não se vê nem
    /// chega à rede.
    /// </summary>
    public static class VisaoRegras
    {
        private readonly record struct Segmento(double X1, double Y1, double X2, double Y2);
        private readonly record struct Fonte(double Cx, double Cy, double Raio);

        /// <summary>
        /// Os tokens que os jogadores da cena recebem. Cena fora de jogo: nenhum. Tokens com dono (heróis e o que o Mestre
        /// deu a alguém) aparecem sempre, como acontece com o dono na tela.
        /// </summary>
        public static HashSet<int> TokensVisiveisAosJogadores(Domain.Entities.Cena cena, IEnumerable<TokenCena> tokens,
            IEnumerable<ParedeCena> paredes, byte[]? neblina)
        {
            var visiveis = new HashSet<int>();
            if (!cena.Ativa) return visiveis;

            var naoOcultos = tokens.Where(t => !t.Oculto).ToList();
            if (!cena.NeblinaAtiva)
            {
                visiveis.UnionWith(naoOcultos.Select(t => t.TokenId));
                return visiveis;
            }

            var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
            var bits = NeblinaRegras.Normalizar(neblina, colunas, linhas);
            double celula = cena.TamanhoCelula;

            // portas abertas e janelas não tapam a visão
            var segmentos = paredes.Where(p => !(p.Porta && p.Aberta) && !p.Janela)
                .Select(p => new Segmento(p.X1 * celula, p.Y1 * celula, p.X2 * celula, p.Y2 * celula))
                .ToList();

            // o raio conta a partir da borda do token (visão em pés; 5 pés por quadrado)
            var fontes = naoOcultos.Where(t => t.Visao > 0).Select(t =>
            {
                var lado = t.Tamanho * celula;
                return new Fonte(t.Coluna * celula + lado / 2, t.Linha * celula + lado / 2, t.Visao / 5.0 * celula + lado / 2);
            }).ToList();

            foreach (var t in naoOcultos)
            {
                if (CenaRegras.Donos(t).Length > 0 || AreaRevelada(bits, colunas, linhas, t))
                {
                    visiveis.Add(t.TokenId);
                    continue;
                }
                var meio = t.Tamanho * celula / 2;
                var cx = t.Coluna * celula + meio;
                var cy = t.Linha * celula + meio;
                if (fontes.Any(f => Math.Sqrt((f.Cx - cx) * (f.Cx - cx) + (f.Cy - cy) * (f.Cy - cy)) <= f.Raio && LinhaLivre(f.Cx, f.Cy, cx, cy, segmentos)))
                    visiveis.Add(t.TokenId);
            }
            return visiveis;
        }

        public static bool EstaRevelado(byte[] bits, int colunas, int linhas, int coluna, int linha)
        {
            if (coluna < 0 || linha < 0 || coluna >= colunas || linha >= linhas) return false;
            var i = linha * colunas + coluna;
            return (bits[i >> 3] & (1 << (i & 7))) != 0;
        }

        /// <summary>Um token aparece se algum dos quadrados que ele ocupa está revelado.</summary>
        private static bool AreaRevelada(byte[] bits, int colunas, int linhas, TokenCena t)
        {
            for (var l = t.Linha; l < t.Linha + t.Tamanho; l++)
                for (var c = t.Coluna; c < t.Coluna + t.Tamanho; c++)
                    if (EstaRevelado(bits, colunas, linhas, c, l)) return true;
            return false;
        }

        /// <summary>Quanto, ao longo do raio (origem + t·direção), ele encontra o segmento; nulo se não encontra.</summary>
        private static double? Cruzamento(double ox, double oy, double dx, double dy, Segmento s)
        {
            var sx = s.X2 - s.X1;
            var sy = s.Y2 - s.Y1;
            var den = dx * sy - dy * sx;
            if (Math.Abs(den) < 1e-9) return null; // paralelos
            var t = ((s.X1 - ox) * sy - (s.Y1 - oy) * sx) / den;
            var u = ((s.X1 - ox) * dy - (s.Y1 - oy) * dx) / den;
            return t >= 0 && u >= -1e-9 && u <= 1 + 1e-9 ? t : null;
        }

        /// <summary>Verdadeiro se nenhuma parede corta o caminho de A até B.</summary>
        private static bool LinhaLivre(double ax, double ay, double bx, double by, List<Segmento> segmentos) =>
            !segmentos.Any(s => Cruzamento(ax, ay, bx - ax, by - ay, s) is { } t && t > 1e-6 && t < 1 - 1e-6);
    }
}
