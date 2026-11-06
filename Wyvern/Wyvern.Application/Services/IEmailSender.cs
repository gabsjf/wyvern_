using System.Threading.Tasks;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Envio de e-mail. O corpo em texto puro sempre vai; com <paramref name="corpoHtml"/> a mensagem sai nas duas formas
    /// e o cliente de e-mail escolhe. A implementação da API enfileira, para o tempo da resposta não depender do SMTP.
    /// </summary>
    public interface IEmailSender
    {
        Task EnviarAsync(string para, string assunto, string corpo, string? corpoHtml = null);
    }
}
