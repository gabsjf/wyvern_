using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Api.Extensions
{
    public static class ApiExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Handler global ativo em todos os ambientes. Devolve ProblemDetails; detalhes internos
        /// (mensagem e stack trace) só aparecem em Development.
        /// </summary>
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, bool incluirDetalhes)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var erro = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExceptionHandler");

                    var (status, titulo, detalhe) = erro switch
                    {
                        RegraNegocioException regra => (StatusCodes.Status400BadRequest, "Requisição inválida.", regra.Message),
                        _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.", (string?)null)
                    };

                    if (status >= 500)
                        logger.LogError(erro, "Erro não tratado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);

                    var problema = new ProblemDetails
                    {
                        Status = status,
                        Title = titulo,
                        Detail = detalhe ?? (incluirDetalhes ? erro?.Message : null)
                    };

                    if (incluirDetalhes && erro != null)
                        problema.Extensions["trace"] = erro.StackTrace;

                    context.Response.StatusCode = status;
                    await context.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json");
                });
            });
        }
    }
}
