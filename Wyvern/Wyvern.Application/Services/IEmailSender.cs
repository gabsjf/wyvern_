using System.Threading.Tasks;

namespace Wyvern.Application.Services
{
    /// <summary>Envio de e-mail em texto puro. A implementação da API enfileira, para o tempo da resposta não depender do SMTP.</summary>
    public interface IEmailSender
    {
        Task EnviarAsync(string para, string assunto, string corpo);
    }
}
