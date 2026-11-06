using System.Net;
using System.Text;

namespace Wyvern.Application.Services
{
    /// <summary>Mesma mensagem em texto puro (para clientes sem HTML) e em HTML.</summary>
    public record CorpoDeEmail(string Texto, string Html);

    /// <summary>
    /// Modelos dos e-mails da conta. O HTML usa tabelas e estilos inline, que é o que os clientes de e-mail aceitam, e as
    /// cores do site. Tudo que vem do usuário (nome) ou de URL passa por HtmlEncode antes de entrar no HTML.
    /// </summary>
    public static class ModeloDeEmail
    {
        public static CorpoDeEmail Confirmacao(string nome, string url) => Montar(
            "Confirme o seu e-mail", nome,
            new[] { "Falta pouco para entrar na guilda. Confirme o seu e-mail para ativar a sua conta no Wyvern." },
            "Confirmar e-mail", url,
            "O link vale por 24 horas. Se você não criou uma conta, ignore esta mensagem.");

        public static CorpoDeEmail Redefinicao(string nome, string url) => Montar(
            "Redefinir a sua senha", nome,
            new[] { "Recebemos um pedido para redefinir a senha da sua conta no Wyvern. Clique no botão abaixo para escolher uma nova senha." },
            "Redefinir senha", url,
            "O link vale por 1 hora e só funciona uma vez. Se não foi você, ignore esta mensagem: a sua senha continua a mesma.");

        public static CorpoDeEmail ContaJaExiste(string nome, string urlLogin) => Montar(
            "Alguém tentou usar o seu e-mail", nome,
            new[]
            {
                "Alguém tentou criar uma conta no Wyvern com este e-mail, mas ele já tem uma conta.",
                "Se foi você, é só entrar com a sua senha. Esqueceu? Use \"Esqueci minha senha\" na tela de login."
            },
            "Ir para o login", urlLogin,
            "Se não reconhece essa tentativa, ignore esta mensagem: nada foi alterado na sua conta.");

        private static CorpoDeEmail Montar(string titulo, string nome, string[] paragrafos, string textoBotao, string url, string rodape)
        {
            var texto = new StringBuilder()
                .Append("Olá, ").Append(nome).Append(".\n\n")
                .Append(string.Join("\n\n", paragrafos)).Append("\n\n")
                .Append(textoBotao).Append(":\n").Append(url).Append("\n\n")
                .Append(rodape)
                .ToString();

            var corpoHtml = new StringBuilder();
            foreach (var p in paragrafos)
                corpoHtml.Append($"<p style=\"margin:0 0 16px;font-size:16px;line-height:24px;color:#3a3531;\">{WebUtility.HtmlEncode(p)}</p>");

            var urlHtml = WebUtility.HtmlEncode(url);
            var html = $@"<!doctype html>
<html lang=""pt-BR"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{WebUtility.HtmlEncode(titulo)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#eeebe5;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#eeebe5;"">
<tr><td align=""center"" style=""padding:32px 16px;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:560px;"">
    <tr><td align=""center"" style=""background-color:#14110f;padding:28px 24px;border-radius:12px 12px 0 0;"">
      <span style=""font-family:'Barlow Condensed','Arial Narrow',Arial,sans-serif;font-size:30px;font-weight:700;letter-spacing:6px;color:#fff6f1;"">WYVERN</span>
      <div style=""height:3px;width:48px;background-color:#b23a48;margin:14px auto 0;line-height:3px;font-size:0;"">&nbsp;</div>
    </td></tr>
    <tr><td style=""background-color:#ffffff;padding:36px 32px;border-radius:0 0 12px 12px;font-family:Barlow,'Segoe UI',Arial,sans-serif;"">
      <h1 style=""margin:0 0 20px;font-size:24px;line-height:30px;color:#14110f;"">{WebUtility.HtmlEncode(titulo)}</h1>
      <p style=""margin:0 0 16px;font-size:16px;line-height:24px;color:#3a3531;"">Olá, {WebUtility.HtmlEncode(nome)}.</p>
      {corpoHtml}
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:28px 0;"">
        <tr><td align=""center"" style=""background-color:#b23a48;border-radius:8px;"">
          <a href=""{urlHtml}"" style=""display:inline-block;padding:14px 32px;font-size:16px;font-weight:600;color:#fff6f1;text-decoration:none;font-family:Barlow,'Segoe UI',Arial,sans-serif;"">{WebUtility.HtmlEncode(textoBotao)}</a>
        </td></tr>
      </table>
      <p style=""margin:0 0 6px;font-size:13px;line-height:20px;color:#7a726b;"">Se o botão não funcionar, copie e cole este endereço no navegador:</p>
      <p style=""margin:0 0 24px;font-size:13px;line-height:20px;word-break:break-all;""><a href=""{urlHtml}"" style=""color:#b23a48;"">{urlHtml}</a></p>
      <hr style=""border:none;border-top:1px solid #e4dfd8;margin:0 0 20px;"">
      <p style=""margin:0;font-size:13px;line-height:20px;color:#7a726b;"">{WebUtility.HtmlEncode(rodape)}</p>
    </td></tr>
    <tr><td align=""center"" style=""padding:20px 16px;font-family:Arial,sans-serif;font-size:12px;color:#7a726b;"">
      Wyvern RPG &middot; Esta é uma mensagem automática, não responda.
    </td></tr>
  </table>
</td></tr>
</table>
</body>
</html>";

            return new CorpoDeEmail(texto, html);
        }
    }
}
