using System.Net;
using System.Text;

namespace Wyvern.Application.Services
{
    /// <summary>Mesma mensagem em texto puro (para clientes sem HTML) e em HTML.</summary>
    public record CorpoDeEmail(string Texto, string Html);

    /// <summary>
    /// Modelos dos e-mails da conta, no visual escuro do site (fundo #14110f, destaque vermelho #b23a48) e com a logo do
    /// dragão. O HTML usa tabelas e estilos inline, que é o que os clientes de e-mail aceitam. A logo é um PNG hospedado
    /// no front (<paramref name="urlLogo"/>). Tudo que vem do usuário (nome) ou de URL passa por HtmlEncode.
    /// </summary>
    public static class ModeloDeEmail
    {
        public static CorpoDeEmail Confirmacao(string nome, string url, string urlLogo) => Montar(
            preheader: "Falta só um clique para ativar a sua conta no Wyvern.",
            eyebrow: "Bem-vindo à guilda",
            titulo: "Confirme o seu e-mail",
            nome: nome,
            paragrafos: new[] { "Que bom ter você por aqui! Para ativar a sua conta e começar a sua primeira campanha, confirme o seu e-mail no botão abaixo." },
            textoBotao: "Confirmar e-mail", url: url, urlLogo: urlLogo,
            tituloNota: "Link válido por 24 horas",
            nota: "Se você não criou uma conta no Wyvern, é só ignorar esta mensagem.");

        public static CorpoDeEmail Redefinicao(string nome, string url, string urlLogo) => Montar(
            preheader: "Crie uma nova senha para a sua conta no Wyvern.",
            eyebrow: "Redefinição de senha",
            titulo: "Esqueceu a senha?",
            nome: nome,
            paragrafos: new[] { "Sem problemas, acontece com os melhores aventureiros. Clique no botão abaixo para escolher uma nova senha para a sua conta." },
            textoBotao: "Criar nova senha", url: url, urlLogo: urlLogo,
            tituloNota: "Link de uso único, válido por 1 hora",
            nota: "Se não foi você que pediu, ignore este e-mail: a sua senha continua a mesma e ninguém mexeu na sua conta.");

        public static CorpoDeEmail ContaJaExiste(string nome, string urlLogin, string urlLogo) => Montar(
            preheader: "Alguém tentou criar uma conta com o seu e-mail.",
            eyebrow: "Aviso de segurança",
            titulo: "Alguém tentou usar o seu e-mail",
            nome: nome,
            paragrafos: new[]
            {
                "Alguém tentou criar uma conta no Wyvern com este e-mail, mas ele já tem uma conta.",
                "Se foi você, é só entrar com a sua senha. Esqueceu? Use \"Esqueci minha senha\" na tela de login."
            },
            textoBotao: "Ir para o login", url: urlLogin, urlLogo: urlLogo,
            tituloNota: "Nada foi alterado",
            nota: "Se você não reconhece essa tentativa, pode ignorar esta mensagem: a sua conta continua como estava.");

        public static CorpoDeEmail TrocaDeEmail(string nome, string url, string urlLogo) => Montar(
            preheader: "Confirme o novo e-mail da sua conta no Wyvern.",
            eyebrow: "Troca de e-mail",
            titulo: "Confirme o novo e-mail",
            nome: nome,
            paragrafos: new[] { "Você pediu para usar este endereço na sua conta do Wyvern. Para concluir a troca, confirme no botão abaixo. Até lá, o e-mail antigo continua valendo para entrar." },
            textoBotao: "Confirmar novo e-mail", url: url, urlLogo: urlLogo,
            tituloNota: "Link válido por 24 horas",
            nota: "Se não foi você que pediu, ignore esta mensagem: nada muda sem o clique.");

        /// <summary>Vai para o e-mail antigo depois da troca. <paramref name="novoEmailMascarado"/> não mostra o endereço inteiro.</summary>
        public static CorpoDeEmail EmailAlterado(string nome, string novoEmailMascarado, string urlLogin, string urlLogo) => Montar(
            preheader: "O e-mail da sua conta no Wyvern foi alterado.",
            eyebrow: "Aviso de segurança",
            titulo: "O seu e-mail foi alterado",
            nome: nome,
            paragrafos: new[]
            {
                $"A sua conta no Wyvern agora usa o e-mail {novoEmailMascarado}. Este endereço não serve mais para entrar.",
                "Se foi você, está tudo certo e não precisa fazer nada."
            },
            textoBotao: "Ir para o Wyvern", url: urlLogin, urlLogo: urlLogo,
            tituloNota: "Não reconhece a troca?",
            nota: "Alguém pode ter a sua senha. Fale com o suporte do Wyvern o quanto antes para recuperar a conta.");

        public static CorpoDeEmail ContaExcluida(string nome, string urlCadastro, string urlLogo) => Montar(
            preheader: "A sua conta no Wyvern foi excluída.",
            eyebrow: "Conta excluída",
            titulo: "Até a próxima aventura",
            nome: nome,
            paragrafos: new[]
            {
                "A sua conta no Wyvern foi excluída, como você pediu. Apagamos os seus dados pessoais, a sua foto e as suas anotações privadas, e encerramos todas as sessões abertas.",
                "Os personagens que você criou continuam nas campanhas dos seus grupos, sem o seu nome. Se quiser voltar um dia, este e-mail pode ser usado num cadastro novo."
            },
            textoBotao: "Criar uma conta nova", url: urlCadastro, urlLogo: urlLogo,
            tituloNota: "Não foi você?",
            nota: "A exclusão exige a senha da conta. Se você não pediu isso, alguém tinha a sua senha: fale com o suporte do Wyvern.");

        private static CorpoDeEmail Montar(string preheader, string eyebrow, string titulo, string nome, string[] paragrafos,
            string textoBotao, string url, string urlLogo, string tituloNota, string nota)
        {
            var texto = new StringBuilder()
                .Append("WYVERN\n\n")
                .Append(titulo.ToUpperInvariant()).Append("\n\n")
                .Append("Olá, ").Append(nome).Append(".\n\n")
                .Append(string.Join("\n\n", paragrafos)).Append("\n\n")
                .Append(textoBotao).Append(":\n").Append(url).Append("\n\n")
                .Append(tituloNota).Append(". ").Append(nota)
                .ToString();

            static string h(string s) => WebUtility.HtmlEncode(s);
            var corpo = new StringBuilder();
            foreach (var p in paragrafos)
                corpo.Append($"<p style=\"margin:0 0 16px;font-size:16px;line-height:26px;color:#d6cdc4;\">{h(p)}</p>");

            var html = $$"""
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="dark">
<meta name="supported-color-schemes" content="dark">
<title>{{h(titulo)}}</title>
<style>
  @media only screen and (max-width:600px) {
    .cartao { padding: 28px 22px !important; }
    .titulo { font-size: 28px !important; line-height: 32px !important; }
    .botao a { display: block !important; padding: 16px 12px !important; }
  }
</style>
</head>
<body style="margin:0;padding:0;background-color:#0e0c0b;">
<div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;font-size:1px;line-height:1px;">{{h(preheader)}}&#8199;&#847;&#8199;&#847;&#8199;&#847;&#8199;&#847;&#8199;&#847;</div>
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#0e0c0b;">
<tr><td align="center" style="padding:36px 16px 40px;">
  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">

    <tr><td align="center" style="padding:0 0 26px;">
      <img src="{{h(urlLogo)}}" width="84" alt="Wyvern" style="display:block;width:84px;height:auto;border:0;margin:0 auto 12px;">
      <span style="font-family:'Barlow Condensed','Arial Narrow',Arial,sans-serif;font-size:22px;font-weight:700;letter-spacing:9px;color:#fff6f1;">WYVERN</span>
    </td></tr>

    <tr><td class="cartao" style="background-color:#1d1916;border:1px solid #2e2823;border-top:3px solid #b23a48;border-radius:14px;padding:40px 40px 36px;font-family:Barlow,'Segoe UI',Helvetica,Arial,sans-serif;">
      <p style="margin:0 0 10px;font-size:12px;line-height:16px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:#e0584b;">{{h(eyebrow)}}</p>
      <h1 class="titulo" style="margin:0 0 24px;font-family:'Barlow Condensed','Arial Narrow',Arial,sans-serif;font-size:34px;line-height:38px;font-weight:700;letter-spacing:.5px;text-transform:uppercase;color:#fff6f1;">{{h(titulo)}}</h1>
      <p style="margin:0 0 16px;font-size:16px;line-height:26px;color:#fff6f1;">Olá, <strong>{{h(nome)}}</strong>.</p>
      {{corpo}}

      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:30px 0;">
        <tr><td class="botao" align="center" style="background-color:#b23a48;border-radius:10px;">
          <a href="{{h(url)}}" style="display:inline-block;padding:16px 44px;font-family:Barlow,'Segoe UI',Helvetica,Arial,sans-serif;font-size:17px;font-weight:700;letter-spacing:.5px;color:#fff6f1;text-decoration:none;">{{h(textoBotao)}}&nbsp;&rarr;</a>
        </td></tr>
      </table>

      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 26px;">
        <tr><td style="background-color:#241f1b;border-left:3px solid #b23a48;border-radius:0 8px 8px 0;padding:14px 18px;">
          <p style="margin:0 0 4px;font-size:13px;line-height:18px;font-weight:700;letter-spacing:.5px;color:#fff6f1;">{{h(tituloNota)}}</p>
          <p style="margin:0;font-size:14px;line-height:22px;color:#b9afa5;">{{h(nota)}}</p>
        </td></tr>
      </table>

      <p style="margin:0 0 6px;font-size:13px;line-height:20px;color:#8a8178;">O botão não funciona? Copie e cole este endereço no navegador:</p>
      <p style="margin:0;font-size:13px;line-height:20px;word-break:break-all;"><a href="{{h(url)}}" style="color:#e0584b;text-decoration:none;">{{h(url)}}</a></p>
    </td></tr>

    <tr><td align="center" style="padding:26px 12px 0;font-family:Barlow,'Segoe UI',Helvetica,Arial,sans-serif;font-size:12px;line-height:19px;color:#7a726b;">
      Wyvern RPG &middot; mensagem automática, não responda este e-mail.<br>
      Você a recebeu porque este endereço foi usado no Wyvern.
    </td></tr>

  </table>
</td></tr>
</table>
</body>
</html>
""";

            return new CorpoDeEmail(texto, html);
        }
    }
}
