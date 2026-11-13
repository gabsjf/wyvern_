namespace Wyvern.Domain.Regras
{
    /// <summary>
    /// O e-mail da conta é guardado e comparado sempre sem espaços e em minúsculas. Assim "Ana@X.com" e "ana@x.com"
    /// são a mesma conta: não dá para criar duas nem pedir a troca para o e-mail de outra pessoa mudando a caixa.
    /// </summary>
    public static class EmailDaConta
    {
        public static string Normalizar(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
