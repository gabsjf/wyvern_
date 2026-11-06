namespace Wyvern.Domain.Exceptions
{
    /// <summary>Login recusado porque o e-mail passou do limite de senhas erradas; vale até <see cref="Espera"/> passar.</summary>
    public class LoginBloqueadoException : RegraNegocioException
    {
        public TimeSpan Espera { get; }

        public LoginBloqueadoException(TimeSpan espera)
            : base($"Muitas tentativas de login. Tente novamente em {Descrever(espera)}.")
        {
            Espera = espera;
        }

        private static string Descrever(TimeSpan espera)
        {
            var segundos = (int)Math.Ceiling(espera.TotalSeconds);
            if (segundos < 60) return $"{segundos} {(segundos == 1 ? "segundo" : "segundos")}";
            var minutos = (int)Math.Ceiling(segundos / 60.0);
            return $"{minutos} {(minutos == 1 ? "minuto" : "minutos")}";
        }
    }
}
