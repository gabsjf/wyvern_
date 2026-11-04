namespace Wyvern.Application.DTOs.Cena
{
    public class NeblinaDto
    {
        public bool Ativa { get; set; }
        public int Colunas { get; set; }
        public int Linhas { get; set; }
        /// <summary>Quadrados revelados: um bit por quadrado, linha a linha, em base64.</summary>
        public string Revelado { get; set; } = string.Empty;
    }

    public class NeblinaAjusteDto
    {
        public bool Ativa { get; set; }
        /// <summary>"revelar" ou "cobrir" para mudar o mapa inteiro de uma vez; vazio não mexe nos quadrados.</summary>
        public string? Tudo { get; set; }
    }

    /// <summary>A névoa de guerra de uma cena: um mapa de bits com um bit por quadrado da grade.</summary>
    public static class NeblinaRegras
    {
        public static (int colunas, int linhas) Dimensoes(Domain.Entities.Cena cena) =>
            (Math.Max(1, cena.Largura / Math.Max(1, cena.TamanhoCelula)), Math.Max(1, cena.Altura / Math.Max(1, cena.TamanhoCelula)));

        private static int Bytes(int colunas, int linhas) => (colunas * linhas + 7) / 8;

        /// <summary>Os dados guardados, ou tudo coberto se não existem ou não batem com a grade atual.</summary>
        public static byte[] Normalizar(byte[]? dados, int colunas, int linhas) =>
            dados != null && dados.Length == Bytes(colunas, linhas) ? dados : new byte[Bytes(colunas, linhas)];

        public static byte[] Tudo(int colunas, int linhas, bool revelar)
        {
            var dados = new byte[Bytes(colunas, linhas)];
            if (revelar) Array.Fill(dados, (byte)0xFF);
            return dados;
        }

        /// <summary>Revela ou cobre o retângulo (em quadrados), cortado nos limites do mapa. Devolve os dados novos.</summary>
        public static byte[] Aplicar(byte[]? dados, int colunas, int linhas, int coluna, int linha, int largura, int altura, bool revelar)
        {
            var novo = (byte[])Normalizar(dados, colunas, linhas).Clone();
            var c0 = Math.Max(0, coluna);
            var l0 = Math.Max(0, linha);
            var c1 = Math.Min(colunas, coluna + Math.Max(0, largura));
            var l1 = Math.Min(linhas, linha + Math.Max(0, altura));

            for (var l = l0; l < l1; l++)
                for (var c = c0; c < c1; c++)
                {
                    var i = l * colunas + c;
                    if (revelar) novo[i >> 3] |= (byte)(1 << (i & 7));
                    else novo[i >> 3] &= (byte)~(1 << (i & 7));
                }
            return novo;
        }

        public static NeblinaDto Montar(Domain.Entities.Cena cena, byte[]? dados)
        {
            var (colunas, linhas) = Dimensoes(cena);
            return new NeblinaDto
            {
                Ativa = cena.NeblinaAtiva, Colunas = colunas, Linhas = linhas,
                Revelado = Convert.ToBase64String(Normalizar(dados, colunas, linhas)),
            };
        }
    }
}
