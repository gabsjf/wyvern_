using System.Globalization;

namespace Wyvern.Domain.Regras
{
    /// <summary>Regras de cálculo do D&amp;D 5e que não dependem de tela nem de banco.</summary>
    public static class RegrasDnd
    {
        /// <summary>
        /// Modificador de atributo: arredonda para baixo, então 9 dá -1 e 7 dá -2.
        /// A divisão inteira do C# arredonda para zero e erraria nas pontuações ímpares abaixo de 10.
        /// </summary>
        public static int Modificador(int pontuacao) => (int)Math.Floor((pontuacao - 10) / 2.0);

        /// <summary>Bônus com sinal para fichas: "+2", "-1", "+0".</summary>
        public static string ComSinal(int valor) =>
            valor >= 0 ? "+" + valor.ToString(CultureInfo.InvariantCulture) : valor.ToString(CultureInfo.InvariantCulture);
    }
}
