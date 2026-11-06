using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// Como a sessão da web é um cookie (enviado pelo navegador sozinho), exige três garantias:
    /// requisições que mudam dados e vêm só com o cookie precisam do cabeçalho X-Requested-With, que obriga o
    /// navegador a fazer o preflight de CORS (outra origem não passa); se trazem Origin, ele tem de ser uma origem permitida
    /// (o navegador o envia em toda mutação e a página não consegue forjá-lo); e os hubs só aceitam conexões de origens permitidas.
    /// Quem autentica com Authorization (app nativo, testes) não depende do cookie e não é afetado.
    /// </summary>
    public class ProtecaoCsrfMiddleware
    {
        private static readonly string[] MetodosSeguros = { "GET", "HEAD", "OPTIONS", "TRACE" };
        private static readonly string[] Hubs = { "/combathub", "/mesahub", "/cenahub" };

        private readonly RequestDelegate _next;
        private readonly string[] _origensPermitidas;

        public ProtecaoCsrfMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _origensPermitidas = CorsConfiguracao.LerOrigens(configuration);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (Hubs.Any(h => request.Path.StartsWithSegments(h)))
            {
                var origem = request.Headers.Origin.ToString();
                if (!string.IsNullOrEmpty(origem) && !_origensPermitidas.Contains(origem, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            else if (!MetodosSeguros.Contains(request.Method, StringComparer.OrdinalIgnoreCase)
                     && !request.Headers.ContainsKey("Authorization")
                     && SessionCookie.TemCookie(request)) // inclui o cookie de refresh: /Auth/refresh e /Auth/logout também mudam estado
            {
                var origem = request.Headers.Origin.ToString();
                var origemEstranha = !string.IsNullOrEmpty(origem) && !_origensPermitidas.Contains(origem, StringComparer.OrdinalIgnoreCase);
                if (origemEstranha || request.Headers[SessionCookie.CabecalhoCsrf] != SessionCookie.ValorCsrf)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            await _next(context);
        }
    }
}
