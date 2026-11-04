using System.Text.RegularExpressions;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Cena
{
    /// <summary>
    /// O deslocamento de um token pelo mapa: quanto ele anda (da ficha) e por onde pode ir. O caminho é de verdade,
    /// quadrado a quadrado (8 direções, a diagonal custa 1 como na régua), contornando paredes, janelas e portas fechadas.
    /// A tela tem uma cópia desta lógica (movimento.ts) para desenhar o alcance; as duas precisam concordar.
    /// </summary>
    public static class MovimentoRegras
    {
        public const double MetrosPorQuadradoPadrao = 1.5;

        /// <summary>O que a ficha mostra quando o deslocamento está em branco (9 m, o padrão do D&D).</summary>
        public const double DeslocamentoPadraoDaFicha = 9;

        /// <summary>
        /// O deslocamento de um personagem, em metros: o que a ficha diz ou, em branco/ilegível, o padrão que a própria
        /// ficha exibe. Token sem personagem não tem ficha: nulo (vale só o que o Mestre escrever no token).
        /// </summary>
        public static double? DaFicha(Domain.Entities.Personagem? personagem) =>
            personagem == null ? null : ParseMetros(personagem.PersonagemCombate?.Deslocamento) ?? DeslocamentoPadraoDaFicha;

        /// <summary>Metros por quadrado da cena (cenas antigas, sem valor, usam o padrão de 1,5 m).</summary>
        public static double MetrosPorQuadrado(Domain.Entities.Cena cena) =>
            cena.MetrosPorQuadrado > 0 ? cena.MetrosPorQuadrado : MetrosPorQuadradoPadrao;

        /// <summary>
        /// Lê o deslocamento escrito na ficha ("9m", "9 m", "9,5 metros", "30 ft", "30 pés") e devolve metros.
        /// Pés valem 0,3 m (5 pés = 1,5 m). Sem unidade, de 1 a 20 é metros e acima disso pés. O que vier
        /// depois do primeiro número ("9m, voo 18m") é ignorado.
        /// </summary>
        public static double? ParseMetros(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var m = Regex.Match(texto, @"(?<n>\d+(?:[.,]\d+)?)\s*(?<u>metros?|mts?|m|p[ée]s?|ft|feet|foot|')?", RegexOptions.IgnoreCase);
            if (!m.Success) return null;

            var numero = double.Parse(m.Groups["n"].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
            var unidade = m.Groups["u"].Value.ToLowerInvariant();
            var emPes = unidade.Length > 0 ? unidade[0] is 'p' or 'f' or '\'' : numero > 20;
            return emPes ? numero * 0.3 : numero;
        }

        /// <summary>Quantos quadrados cabem em tantos metros (inteiros, para baixo).</summary>
        public static int Quadrados(double metros, double metrosPorQuadrado) =>
            (int)Math.Floor(metros / metrosPorQuadrado + 1e-9);

        /// <summary>Índice espacial das paredes que barram o movimento, para não testar todas a cada passo.</summary>
        private sealed class Barreiras
        {
            private readonly Dictionary<long, List<ParedeCena>> _celulas = new();

            public Barreiras(IEnumerable<ParedeCena> paredes)
            {
                foreach (var p in paredes.Where(p => !(p.Porta && p.Aberta)))
                {
                    // cada parede entra nas células que ela pode tocar (com uma de folga, por causa das bordas)
                    var c0 = Math.Min(p.X1, p.X2) - 1; var c1 = Math.Max(p.X1, p.X2);
                    var l0 = Math.Min(p.Y1, p.Y2) - 1; var l1 = Math.Max(p.Y1, p.Y2);
                    for (var l = l0; l <= l1; l++)
                        for (var c = c0; c <= c1; c++)
                        {
                            var chave = Chave(c, l);
                            if (!_celulas.TryGetValue(chave, out var lista)) _celulas[chave] = lista = new List<ParedeCena>();
                            lista.Add(p);
                        }
                }
            }

            private static long Chave(int c, int l) => ((long)l << 32) | (uint)c;

            public IEnumerable<ParedeCena> Em(int c, int l) =>
                _celulas.TryGetValue(Chave(c, l), out var lista) ? lista : Enumerable.Empty<ParedeCena>();
        }

        /// <summary>Alguma parede passa pelo interior do retângulo do token? (token de tamanho par "dividido" por uma parede)</summary>
        private static bool Dividido(Barreiras barreiras, int tamanho, int coluna, int linha)
        {
            if (tamanho <= 1) return false;
            for (var j = 0; j < tamanho; j++)
                for (var i = 0; i < tamanho; i++)
                    foreach (var p in barreiras.Em(coluna + i, linha + j))
                        if (AtravessaInterior(p, coluna, linha, tamanho)) return true;
            return false;
        }

        /// <summary>O segmento tem algum trecho estritamente dentro do retângulo (Liang-Barsky no retângulo fechado).</summary>
        private static bool AtravessaInterior(ParedeCena p, double x0, double y0, double lado)
        {
            double t0 = 0, t1 = 1;
            double dx = p.X2 - p.X1, dy = p.Y2 - p.Y1;
            bool Recortar(double d, double distancia)
            {
                if (d == 0) return distancia >= 0;
                var t = distancia / d;
                if (d < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
                else { if (t < t0) return false; if (t < t1) t1 = t; }
                return true;
            }
            if (!Recortar(-dx, p.X1 - x0) || !Recortar(dx, x0 + lado - p.X1) || !Recortar(-dy, p.Y1 - y0) || !Recortar(dy, y0 + lado - p.Y1))
                return false;
            if (t0 >= t1) return false;
            var mx = p.X1 + dx * (t0 + t1) / 2;
            var my = p.Y1 + dy * (t0 + t1) / 2;
            return mx > x0 && mx < x0 + lado && my > y0 && my < y0 + lado;
        }

        /// <summary>Um passo (dc, dl) com o token inteiro: o centro de cada quadrado que ele ocupa não pode cruzar nenhuma parede.</summary>
        private static bool PassoLivre(Barreiras barreiras, int tamanho, int coluna, int linha, int dc, int dl)
        {
            for (var j = 0; j < tamanho; j++)
                for (var i = 0; i < tamanho; i++)
                {
                    double ax = coluna + i + 0.5, ay = linha + j + 0.5, bx = ax + dc, by = ay + dl;
                    var candidatas = barreiras.Em(coluna + i, linha + j).Concat(barreiras.Em(coluna + i + dc, linha + j + dl));
                    if (candidatas.Any(p => CenaRegras.Barra(ax, ay, bx, by, p.X1, p.Y1, p.X2, p.Y2))) return true == false;
                }
            return true;
        }

        /// <summary>
        /// Distância, em quadrados, de cada posição que o token alcança a partir de (coluna, linha), até `limite`.
        /// Posições são o canto superior esquerdo do token. Para cedo se `alvo` for alcançado.
        /// </summary>
        public static Dictionary<(int coluna, int linha), int> Alcance(
            Domain.Entities.Cena cena, int tamanho, int coluna, int linha, IEnumerable<ParedeCena> paredes, int limite, (int coluna, int linha)? alvo = null)
        {
            var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
            var barreiras = new Barreiras(paredes);
            var distancias = new Dictionary<(int, int), int> { [(coluna, linha)] = 0 };
            if (alvo == (coluna, linha)) return distancias;

            var fila = new Queue<(int c, int l)>();
            fila.Enqueue((coluna, linha));
            while (fila.Count > 0)
            {
                var (c, l) = fila.Dequeue();
                var d = distancias[(c, l)];
                if (d >= limite) continue;

                for (var dl = -1; dl <= 1; dl++)
                    for (var dc = -1; dc <= 1; dc++)
                    {
                        if (dc == 0 && dl == 0) continue;
                        int nc = c + dc, nl = l + dl;
                        if (nc < 0 || nl < 0 || nc + tamanho > colunas || nl + tamanho > linhas) continue;
                        if (distancias.ContainsKey((nc, nl))) continue;
                        if (Dividido(barreiras, tamanho, nc, nl) || !PassoLivre(barreiras, tamanho, c, l, dc, dl)) continue;

                        distancias[(nc, nl)] = d + 1;
                        if (alvo == (nc, nl)) return distancias;
                        fila.Enqueue((nc, nl));
                    }
            }
            return distancias;
        }

        /// <summary>Quantos quadrados custa ir de uma posição a outra pelo caminho mais curto; nulo se não há caminho (ou passa do limite).</summary>
        public static int? Custo(
            Domain.Entities.Cena cena, int tamanho, int coluna, int linha, int destinoColuna, int destinoLinha, IEnumerable<ParedeCena> paredes, int limite = int.MaxValue)
        {
            var alvo = (destinoColuna, destinoLinha);
            var alcance = Alcance(cena, tamanho, coluna, linha, paredes, limite, alvo);
            return alcance.TryGetValue(alvo, out var custo) ? custo : null;
        }
    }
}
