namespace Wyvern.Domain.Exceptions
{
    /// <summary>
    /// Login com a senha certa numa conta que ainda não confirmou o e-mail. Só aparece para quem acertou a senha,
    /// e a resposta leva um código para o front oferecer um link novo.
    /// </summary>
    public class EmailNaoConfirmadoException : RegraNegocioException
    {
        public const string Codigo = "email-nao-confirmado";

        public EmailNaoConfirmadoException()
            : base("Confirme o seu e-mail para entrar. Não achou o link ou ele venceu? Peça outro.")
        {
        }
    }
}
