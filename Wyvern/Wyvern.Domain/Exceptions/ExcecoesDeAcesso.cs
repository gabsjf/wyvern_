namespace Wyvern.Domain.Exceptions
{
    /// <summary>O recurso não existe ou o usuário não tem como saber que existe (responde 404).</summary>
    public class NaoEncontradoException : Exception
    {
        public NaoEncontradoException(string message) : base(message)
        {
        }
    }

    /// <summary>O usuário conhece o recurso mas não tem permissão para a ação (responde 403).</summary>
    public class AcessoNegadoException : Exception
    {
        public AcessoNegadoException(string message) : base(message)
        {
        }
    }
}
