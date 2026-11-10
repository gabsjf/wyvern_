using System;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Transforma o User-Agent numa descrição curta para a lista de aparelhos conectados ("Chrome em Windows").
    /// É só um rótulo para a pessoa reconhecer o login; nada aqui decide acesso, e o User-Agent não é confiável.
    /// </summary>
    public static class DescritorDeDispositivo
    {
        private const int Maximo = 120;

        public static string Descrever(string? userAgent, bool appNativo)
        {
            var ua = userAgent ?? string.Empty;
            var sistema = Sistema(ua);

            string descricao;
            if (appNativo)
                descricao = sistema != null ? $"App Wyvern ({sistema})" : "App Wyvern";
            else
            {
                var navegador = Navegador(ua);
                descricao = (navegador, sistema) switch
                {
                    (not null, not null) => $"{navegador} em {sistema}",
                    (not null, null) => navegador,
                    (null, not null) => $"Navegador em {sistema}",
                    _ => "Aparelho desconhecido"
                };
            }
            return descricao.Length > Maximo ? descricao[..Maximo] : descricao;
        }

        // A ordem importa: Edge e Opera também dizem "Chrome", e o Chrome também diz "Safari".
        private static string? Navegador(string ua)
        {
            if (Tem(ua, "Edg/") || Tem(ua, "EdgA/") || Tem(ua, "EdgiOS/")) return "Edge";
            if (Tem(ua, "OPR/") || Tem(ua, "Opera")) return "Opera";
            if (Tem(ua, "Firefox/") || Tem(ua, "FxiOS/")) return "Firefox";
            if (Tem(ua, "Chrome/") || Tem(ua, "CriOS/")) return "Chrome";
            if (Tem(ua, "Safari/")) return "Safari";
            return null;
        }

        // iPhone e iPad dizem "like Mac OS X", e o Android diz "Linux": os mais específicos vêm primeiro.
        private static string? Sistema(string ua)
        {
            if (Tem(ua, "Android")) return "Android";
            if (Tem(ua, "iPhone") || Tem(ua, "iPad") || Tem(ua, "iPod")) return "iOS";
            if (Tem(ua, "Windows")) return "Windows";
            if (Tem(ua, "CrOS")) return "ChromeOS";
            if (Tem(ua, "Macintosh") || Tem(ua, "Mac OS X")) return "macOS";
            if (Tem(ua, "Linux")) return "Linux";
            return null;
        }

        private static bool Tem(string ua, string trecho) => ua.Contains(trecho, StringComparison.OrdinalIgnoreCase);
    }
}
