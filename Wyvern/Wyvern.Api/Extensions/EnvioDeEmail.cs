using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Wyvern.Application.Services;

namespace Wyvern.Api.Extensions
{
    public record MensagemEmail(string Para, string Assunto, string Corpo, string? CorpoHtml = null);

    /// <summary>Quem de fato entrega a mensagem (SMTP em produção, log em desenvolvimento).</summary>
    public interface ITransporteDeEmail
    {
        Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct);
    }

    /// <summary>
    /// SMTP configurado em Email:Host, Porta, Usuario, Senha, Remetente e NomeRemetente (variáveis Email__*).
    /// Na porta 587 usa STARTTLS; na 465, TLS direto.
    /// </summary>
    public class SmtpTransporteDeEmail : ITransporteDeEmail
    {
        private readonly string _host, _usuario, _senha, _remetente, _nomeRemetente;
        private readonly int _porta;

        public SmtpTransporteDeEmail(IConfiguration configuration)
        {
            _host = configuration["Email:Host"]!;
            _usuario = configuration["Email:Usuario"]!;
            _senha = configuration["Email:Senha"]!;
            _remetente = configuration["Email:Remetente"]!;
            _nomeRemetente = configuration["Email:NomeRemetente"] ?? "Wyvern";
            _porta = configuration.GetValue("Email:Porta", 587);
        }

        public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct)
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_nomeRemetente, _remetente));
            mime.To.Add(MailboxAddress.Parse(mensagem.Para));
            mime.Subject = mensagem.Assunto;
            // Texto puro sempre; com HTML a mensagem vai nas duas formas (multipart/alternative).
            mime.Body = new BodyBuilder { TextBody = mensagem.Corpo, HtmlBody = mensagem.CorpoHtml }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _porta, _porta == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(_usuario, _senha, ct);
            await smtp.SendAsync(mime, ct);
            await smtp.DisconnectAsync(true, ct);
        }
    }

    /// <summary>Sem SMTP configurado (só em desenvolvimento): escreve a mensagem no log, com o link de confirmação.</summary>
    public class LogTransporteDeEmail : ITransporteDeEmail
    {
        private readonly ILogger<LogTransporteDeEmail> _logger;

        public LogTransporteDeEmail(ILogger<LogTransporteDeEmail> logger) => _logger = logger;

        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct)
        {
            _logger.LogInformation("E-mail (SMTP não configurado) para {Para}: {Assunto}\n{Corpo}", mensagem.Para, mensagem.Assunto, mensagem.Corpo);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Enfileira e devolve na hora: o SMTP leva segundos, e esperar por ele deixaria a resposta do cadastro mais lenta
    /// justamente quando o e-mail já existe ou não, o que revelaria quem tem conta. Uma falha de envio é só registrada.
    /// </summary>
    public class FilaDeEmails : BackgroundService, IEmailSender
    {
        private readonly Channel<MensagemEmail> _fila = Channel.CreateUnbounded<MensagemEmail>();
        private readonly ITransporteDeEmail _transporte;
        private readonly ILogger<FilaDeEmails> _logger;

        public FilaDeEmails(ITransporteDeEmail transporte, ILogger<FilaDeEmails> logger)
        {
            _transporte = transporte;
            _logger = logger;
        }

        public Task EnviarAsync(string para, string assunto, string corpo, string? corpoHtml = null)
        {
            _fila.Writer.TryWrite(new MensagemEmail(para, assunto, corpo, corpoHtml));
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var mensagem in _fila.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _transporte.EnviarAsync(mensagem, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Falha ao enviar o e-mail \"{Assunto}\".", mensagem.Assunto);
                }
            }
        }
    }

    public static class EnvioDeEmailExtensions
    {
        private static readonly string[] ChavesObrigatorias = { "Email:Host", "Email:Usuario", "Email:Senha", "Email:Remetente", "Email:UrlFront" };

        /// <summary>Fora de desenvolvimento, SMTP incompleto derruba a inicialização (sem ele ninguém consegue confirmar a conta).</summary>
        public static IServiceCollection AddEnvioDeEmail(this IServiceCollection services, IConfiguration configuration, bool desenvolvimento)
        {
            var smtpConfigurado = !string.IsNullOrWhiteSpace(configuration["Email:Host"]);
            if (!desenvolvimento)
            {
                var faltando = ChavesObrigatorias.Where(c => string.IsNullOrWhiteSpace(configuration[c])).ToArray();
                if (faltando.Length > 0)
                    throw new InvalidOperationException($"Configure o envio de e-mail: faltam {string.Join(", ", faltando)} (variáveis com __ no lugar de :).");
            }

            if (smtpConfigurado)
                services.AddSingleton<ITransporteDeEmail, SmtpTransporteDeEmail>();
            else
                services.AddSingleton<ITransporteDeEmail, LogTransporteDeEmail>();

            services.AddSingleton<FilaDeEmails>();
            services.AddSingleton<IEmailSender>(p => p.GetRequiredService<FilaDeEmails>());
            services.AddHostedService(p => p.GetRequiredService<FilaDeEmails>());
            return services;
        }
    }
}
