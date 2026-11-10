using System.Text.RegularExpressions;
using Sentry;

namespace Wyvern.Api.Extensions
{
    /// <summary>
    /// Monitoramento de erros com o Sentry. Só liga quando "Sentry:Dsn" está configurado (variável Sentry__Dsn): sem ele,
    /// nada sai da máquina, e é assim nos testes e no desenvolvimento.
    /// O que vai: erros registrados como Error ou Critical (os 500 do tratador global, falha de e-mail, erro em hub), com a
    /// rota, o ambiente e o id do usuário. O que não vai: e-mail, IP, cookies, Authorization e os tokens de link/convite.
    /// </summary>
    public static class SentryConfiguracao
    {
        // Tokens de link (confirmação, redefinição, troca de e-mail) e de convite aparecem na URL e dão acesso: nunca saem.
        private static readonly Regex TokenNaQuery = new(@"(?i)(token|refreshToken|access_token|ticket)=[^&#\s]*", RegexOptions.Compiled);
        private static readonly Regex TokenDeConvite = new(@"(?i)/join/[^/?#\s]+", RegexOptions.Compiled);
        private static readonly string[] CabecalhosSensiveis = { "Authorization", "Cookie", "Set-Cookie", "X-Wyvern-Refresh" };

        public static bool Ativo(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["Sentry:Dsn"]);

        public static void AddMonitoramentoDeErros(this WebApplicationBuilder builder)
        {
            if (!Ativo(builder.Configuration)) return;

            builder.WebHost.UseSentry(o =>
            {
                o.Dsn = builder.Configuration["Sentry:Dsn"];
                o.Environment = builder.Configuration["Sentry:Environment"] ?? builder.Environment.EnvironmentName.ToLowerInvariant();
                o.Release = builder.Configuration["Sentry:Release"]; // ex.: o commit do deploy; nulo = o Sentry usa a versão do assembly
                o.SendDefaultPii = false; // sem IP, cookies e nome de usuário automáticos

                // Só erros de verdade viram evento; o resto entra como rastro (breadcrumb) a partir de Warning, para o SQL
                // e os logs de informação (que em dev incluem o corpo dos e-mails) não irem junto.
                o.MinimumEventLevel = LogLevel.Error;
                o.MinimumBreadcrumbLevel = LogLevel.Warning;

                // Desempenho custa cota à parte; fica desligado até alguém pedir (Sentry:TracesSampleRate, de 0 a 1).
                o.TracesSampleRate = double.TryParse(builder.Configuration["Sentry:TracesSampleRate"], System.Globalization.CultureInfo.InvariantCulture, out var taxa) ? taxa : 0;

                o.SetBeforeSend((evento, _) => Limpar(evento));
                o.SetBeforeBreadcrumb((rastro, _) =>
                {
                    if (rastro.Message != null) rastro = new Breadcrumb(LimparTexto(rastro.Message)!, rastro.Type, rastro.Data?.ToDictionary(k => k.Key, k => LimparTexto(k.Value) ?? string.Empty), rastro.Category, rastro.Level);
                    return rastro;
                });
            });
        }

        /// <summary>Depois da autenticação: marca o evento com o id da conta (só o número, nada de nome ou e-mail).</summary>
        public static void UseMonitoramentoDeErros(this WebApplication app)
        {
            if (!Ativo(app.Configuration)) return;

            app.Use(async (context, next) =>
            {
                var id = context.User.FindFirst("nameid")?.Value;
                if (id != null)
                    SentrySdk.ConfigureScope(scope => scope.User = new SentryUser { Id = id });
                await next();
            });
        }

        private static SentryEvent Limpar(SentryEvent evento)
        {
            var pedido = evento.Request;
            pedido.Cookies = null;
            foreach (var nome in CabecalhosSensiveis)
                pedido.Headers.Remove(nome);
            pedido.QueryString = LimparTexto(pedido.QueryString);
            pedido.Url = LimparTexto(pedido.Url);
            pedido.Data = null; // corpo do pedido: pode ter senha, e-mail, anotação

            if (evento.Message?.Formatted is { } mensagem)
                evento.Message = new SentryMessage { Formatted = LimparTexto(mensagem), Message = evento.Message.Message, Params = null };

            evento.User.IpAddress = null;
            evento.User.Email = null;
            evento.User.Username = null;
            return evento;
        }

        /// <summary>Tira tokens de link e de convite de URLs e mensagens (público para os testes).</summary>
        public static string? LimparTexto(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            texto = TokenNaQuery.Replace(texto, m => $"{m.Groups[1].Value}=[removido]");
            return TokenDeConvite.Replace(texto, "/join/[removido]");
        }
    }
}
