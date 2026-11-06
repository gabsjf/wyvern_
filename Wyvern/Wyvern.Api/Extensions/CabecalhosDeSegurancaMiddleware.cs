using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// Cabeçalhos que limitam o estrago de um XSS e de sniffing de conteúdo. A API só devolve JSON e PDF, então a CSP
    /// proíbe tudo (default-src 'none'). Os cabeçalhos entram em OnStarting para cobrir também 403, 429 e erros.
    /// O HSTS fica em UseHsts (só em produção).
    /// </summary>
    public class CabecalhosDeSegurancaMiddleware
    {
        private const string Csp = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _ambiente;

        public CabecalhosDeSegurancaMiddleware(RequestDelegate next, IHostEnvironment ambiente)
        {
            _next = next;
            _ambiente = ambiente;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var cabecalhos = context.Response.Headers;
                cabecalhos["X-Content-Type-Options"] = "nosniff";
                cabecalhos["Referrer-Policy"] = "no-referrer";
                if (!PaginaDeDocumentacao(context.Request.Path))
                    cabecalhos["Content-Security-Policy"] = Csp;
                return Task.CompletedTask;
            });

            return _next(context);
        }

        // O Scalar (só em desenvolvimento) carrega scripts e estilos inline e de CDN; não existe em produção.
        private bool PaginaDeDocumentacao(PathString caminho) =>
            _ambiente.IsDevelopment()
            && (caminho.StartsWithSegments("/scalar") || caminho.StartsWithSegments("/openapi"));
    }

    public static class CabecalhosDeSegurancaExtensions
    {
        public static IApplicationBuilder UseCabecalhosDeSeguranca(this IApplicationBuilder app) =>
            app.UseMiddleware<CabecalhosDeSegurancaMiddleware>();
    }
}
