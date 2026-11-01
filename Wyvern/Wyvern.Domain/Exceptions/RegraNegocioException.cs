namespace Wyvern.Domain.Exceptions
{
    /// <summary>
    /// Violação de regra de negócio cuja mensagem é segura para ser exibida ao usuário.
    /// Qualquer outra exceção é tratada como erro interno e não vaza detalhes.
    /// </summary>
    public class RegraNegocioException : Exception
    {
        public RegraNegocioException(string message) : base(message)
        {
        }
    }
}
