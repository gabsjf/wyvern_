using System.Text;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// O campo "História e Personalidade" guarda seções como linhas "Título: conteúdo" (é assim que as importações
    /// gravam e que a ficha mostra). Uma seção vai até a próxima linha que começa com um título conhecido, então o
    /// conteúdo pode ter várias linhas. O que vem antes de qualquer título é texto livre.
    /// </summary>
    public static class HistoriaEmSecoes
    {
        public const string TracosDeEspecie = "Traços de Espécie";
        public const string Talentos = "Talentos";
        public const string Tracos = "Traços";
        public const string Ideais = "Ideais";
        public const string Vinculos = "Vínculos";
        public const string Defeitos = "Defeitos";
        public const string Historia = "História";

        // "Traços de Espécie" antes de "Traços": o mais longo ganha.
        private static readonly string[] Titulos = { TracosDeEspecie, Talentos, Tracos, Ideais, Vinculos, Defeitos, Historia };

        public sealed record Resultado(IReadOnlyDictionary<string, string> Secoes, string Livre)
        {
            public string De(string titulo) => Secoes.TryGetValue(titulo, out var v) ? v : "";
            public bool TemSecoes => Secoes.Count > 0;
        }

        public static Resultado Ler(string? texto)
        {
            var secoes = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
            var livre = new StringBuilder();
            StringBuilder? atual = null;

            foreach (var linha in (texto ?? "").Replace("\r", "").Split('\n'))
            {
                var titulo = Titulos.FirstOrDefault(t => linha.TrimStart().StartsWith(t + ":", StringComparison.OrdinalIgnoreCase));
                if (titulo != null)
                {
                    if (!secoes.TryGetValue(titulo, out atual)) secoes[titulo] = atual = new StringBuilder();
                    else atual.Append('\n');
                    atual.Append(linha.TrimStart()[(titulo.Length + 1)..].Trim());
                }
                else (atual ?? livre).Append(atual == null && livre.Length == 0 ? "" : "\n").Append(linha);
            }

            var limpas = secoes
                .Select(kv => (kv.Key, Valor: kv.Value.ToString().Trim()))
                .Where(x => x.Valor != "")
                .ToDictionary(x => x.Key, x => x.Valor, StringComparer.OrdinalIgnoreCase);
            return new Resultado(limpas, livre.ToString().Trim());
        }

        /// <summary>Monta o texto: primeiro o texto livre, depois uma linha "Título: conteúdo" por seção preenchida.</summary>
        public static string Montar(string? livre, params (string Titulo, string? Valor)[] secoes)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(livre)) partes.Add(livre.Trim());
            partes.AddRange(secoes.Where(s => !string.IsNullOrWhiteSpace(s.Valor)).Select(s => $"{s.Titulo}: {s.Valor!.Trim()}"));
            return string.Join("\n", partes);
        }
    }
}
