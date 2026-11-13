using System.ComponentModel.DataAnnotations;

namespace Wyvern.Domain.Entities
{
    /// <summary>
    /// Um modelo 3D (.glb) da biblioteca da campanha. "Cenario" é peça para pôr no mapa (árvore, mesa, estátua);
    /// "Miniatura" é a figura de um personagem ou token na visão 3D.
    /// </summary>
    public class Modelo3d
    {
        public const string Cenario = "Cenario";
        public const string Miniatura = "Miniatura";

        /// <summary>Como ler as medidas do arquivo (ver a propriedade Escala).</summary>
        public const string EscalaAuto = "Auto", EscalaImpressao = "Impressao", EscalaReal = "Real", EscalaEncaixar = "Encaixar";

        [Key]
        public int ModeloId { get; set; }

        public int CampanhaId { get; set; }
        public Campanha? Campanha { get; set; }

        [Required, StringLength(80)]
        public string Nome { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Categoria { get; set; } = Cenario;

        /// <summary>
        /// Como as medidas do arquivo viram quadrados na mesa (25,4 mm de miniatura = 1 quadrado = 1,5 m de verdade):
        /// "Impressao" = miniatura de impressão em mm; "Real" = tamanho real em metros; "Encaixar" = cabe no token;
        /// "Auto" = o 3D deduz pela altura.
        /// </summary>
        [Required, StringLength(20)]
        public string Escala { get; set; } = EscalaAuto;

        /// <summary>Tamanho do arquivo em bytes (para a lista e para o limite da campanha, sem carregar os bytes).</summary>
        public long Tamanho { get; set; }

        /// <summary>Quem subiu. Um jogador só pode apagar o que ele mesmo subiu.</summary>
        public int? EnviadoPorId { get; set; }
        public Usuario? EnviadoPor { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Os bytes do .glb, em tabela própria para a lista da biblioteca não carregá-los.</summary>
    public class Modelo3dArquivo
    {
        [Key]
        public int ModeloId { get; set; }
        public Modelo3d? Modelo { get; set; }

        public byte[] Dados { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Uma peça de cenário posta no mapa, montada no editor 3D como numa engine: posição livre, giro nos três eixos e
    /// escala por eixo, tudo em quadrados. Coluna, linha e tamanho são o quadrado que ela ocupa, para o mapa 2D e a névoa.
    /// </summary>
    public class ObjetoCena
    {
        [Key]
        public int ObjetoId { get; set; }

        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        public int ModeloId { get; set; }
        public Modelo3d? Modelo { get; set; }

        /// <summary>Quadrado do canto superior esquerdo da área que a peça ocupa (tirado da posição e da escala).</summary>
        public int Coluna { get; set; }
        public int Linha { get; set; }

        /// <summary>Lado da área ocupada, em quadrados inteiros (o maior entre EscalaX e EscalaZ, arredondado).</summary>
        public int Tamanho { get; set; } = 1;

        /// <summary>Centro da base da peça, em quadrados (com fração); Y é a altura acima do chão, para empilhar.</summary>
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        /// <summary>Tamanho em quadrados em cada eixo: 1 = o lado maior da peça, visto de cima, ocupa um quadrado.</summary>
        public double EscalaX { get; set; } = 1;
        public double EscalaY { get; set; } = 1;
        public double EscalaZ { get; set; } = 1;

        /// <summary>Giro em graus em volta do eixo vertical, no sentido horário visto de cima (0 a 360).</summary>
        public double Rotacao { get; set; }

        /// <summary>Inclinação em graus nos eixos deitados (tombar um tronco, inclinar uma pedra), de -180 a 180.</summary>
        public double GiroX { get; set; }
        public double GiroZ { get; set; }

        /// <summary>Escondido dos jogadores até o Mestre revelar.</summary>
        public bool Oculto { get; set; }
    }
}
