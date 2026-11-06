using Microsoft.Extensions.Configuration;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// Lê e valida Cors:AllowedOrigins. Com cookies (AllowCredentials) o CORS não aceita "*" e o navegador compara a origem
    /// byte a byte, então a lista tem de ser exata. Entrada inválida derruba a inicialização com uma mensagem clara, em vez de
    /// virar um 500 na primeira requisição (ou, pior, uma origem que nunca casa e ninguém percebe).
    /// </summary>
    public static class CorsConfiguracao
    {
        public const string Chave = "Cors:AllowedOrigins";

        public static string[] LerOrigens(IConfiguration configuration) =>
            Validar(configuration.GetSection(Chave).Get<string[]>() ?? Array.Empty<string>());

        public static string[] Validar(IEnumerable<string> origens)
        {
            var validas = new List<string>();
            foreach (var origem in origens)
            {
                var motivo = MotivoDeRejeicao(origem);
                if (motivo != null)
                    throw new InvalidOperationException($"{Chave}: \"{origem}\" é inválida: {motivo}");
                validas.Add(origem);
            }
            return validas.ToArray();
        }

        private static string? MotivoDeRejeicao(string origem)
        {
            if (string.IsNullOrWhiteSpace(origem))
                return "entrada vazia.";
            if (origem.Contains('*'))
                return "curingas não são aceitos com cookies; liste cada origem (esquema://host[:porta]).";
            if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
                return "use esquema://host[:porta].";
            if (origem.EndsWith('/') || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return "origem não tem barra final, caminho, query nem fragmento (o navegador envia só esquema://host[:porta]).";
            if (uri.Scheme == Uri.UriSchemeHttp && !EhLoopback(uri.Host))
                return "http só é aceito para localhost; em produção use https.";
            return null;
        }

        private static bool EhLoopback(string host) =>
            host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
    }
}
