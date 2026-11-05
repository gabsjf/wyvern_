using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// Como a sessão da web é um cookie (enviado pelo navegador sozinho), exige duas garantias:
    /// requisições que mudam dados e vêm só com o cookie precisam do cabeçalho X-Requested-With, que obriga o
    /// navegador a fazer o preflight de CORS (outra origem não passa); e os hubs só aceitam conexões de origens permitidas.
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
            _origensPermitidas = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
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
                     && request.Cookies.ContainsKey(SessionCookie.Nome)
                     && request.Headers[SessionCookie.CabecalhoCsrf] != SessionCookie.ValorCsrf)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await _next(context);
        }
    }
}
