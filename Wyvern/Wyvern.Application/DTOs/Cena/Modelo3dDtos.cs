using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Cena
{
    /// <summary>Limites da biblioteca 3D (seção "Modelos3d" da configuração). Produção usa o padrão; o local pode subir.</summary>
    public class Modelos3dOptions
    {
        public const string Secao = "Modelos3d";
        public int TamanhoMaximoMb { get; set; } = 15;
        public int EspacoPorCampanhaMb { get; set; } = 300;

        public long TamanhoMaximo => Math.Min(TamanhoMaximoMb, ArquivoGlb.TetoDaRequisicaoMb) * 1024L * 1024;
        public long EspacoPorCampanha => EspacoPorCampanhaMb * 1024L * 1024;
    }

    /// <summary>Regras dos arquivos .glb (glTF binário): um arquivo só, com as texturas dentro.</summary>
    public static class ArquivoGlb
    {
        /// <summary>Teto fixo do tamanho da requisição de envio (o limite que vale é o da configuração, até este teto).</summary>
        public const int TetoDaRequisicaoMb = 200;
        public const long TetoDaRequisicao = TetoDaRequisicaoMb * 1024L * 1024 + 64 * 1024;
        public const int ObjetosPorCena = 300;

        /// <summary>
        /// Confere o cabeçalho do glTF binário: "glTF", versão 2 e o tamanho declarado igual ao do arquivo, e que o
        /// primeiro bloco é o JSON. O nome e o tipo que o cliente informa não valem.
        /// </summary>
        public static bool Valido(byte[] b)
        {
            if (b.Length < 20) return false;
            if (b[0] != (byte)'g' || b[1] != (byte)'l' || b[2] != (byte)'T' || b[3] != (byte)'F') return false;
            if (BitConverter.ToUInt32(b, 4) != 2) return false;
            if (BitConverter.ToUInt32(b, 8) != (uint)b.Length) return false;
            var tamanhoJson = BitConverter.ToUInt32(b, 12);
            var tipoJson = BitConverter.ToUInt32(b, 16);
            return tipoJson == 0x4E4F534A && tamanhoJson > 0 && 20 + (long)tamanhoJson <= b.Length; // "JSON"
        }

        public static bool CategoriaValida(string? categoria) => categoria is Modelo3d.Cenario or Modelo3d.Miniatura;

        public static bool EscalaValida(string? escala) =>
            escala is Modelo3d.EscalaAuto or Modelo3d.EscalaImpressao or Modelo3d.EscalaReal or Modelo3d.EscalaEncaixar;
    }

    public class Modelo3dFormDto
    {
        [Required(ErrorMessage = "Escolha o arquivo .glb.")]
        public IFormFile? Arquivo { get; set; }

        [Required(ErrorMessage = "Dê um nome ao modelo."), StringLength(80, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>"Cenario" ou "Miniatura".</summary>
        [Required]
        public string Categoria { get; set; } = Modelo3d.Cenario;

        /// <summary>"Auto" (padrão), "Impressao", "Real" ou "Encaixar".</summary>
        public string? Escala { get; set; }
    }

    /// <summary>Uma versão nova do arquivo (a otimizada) para um modelo que já está na biblioteca.</summary>
    public class Modelo3dSubstituicaoDto
    {
        [Required(ErrorMessage = "Escolha o arquivo .glb.")]
        public IFormFile? Arquivo { get; set; }
    }

    public class Modelo3dDto
    {
        public int ModeloId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = Modelo3d.Cenario;
        public string Escala { get; set; } = Modelo3d.EscalaAuto;
        public long Tamanho { get; set; }
        public int? EnviadoPorId { get; set; }
        public string? EnviadoPorNome { get; set; }
        public DateTime CriadoEm { get; set; }

        public static Modelo3dDto De(Modelo3d m) => new()
        {
            ModeloId = m.ModeloId, Nome = m.Nome, Categoria = m.Categoria, Escala = m.Escala, Tamanho = m.Tamanho,
            EnviadoPorId = m.EnviadoPorId, EnviadoPorNome = m.EnviadoPor?.Nome, CriadoEm = m.CriadoEm,
        };
    }

    /// <summary>A biblioteca inteira, com o espaço usado (o limite vale por campanha).</summary>
    public class BibliotecaDeModelosDto
    {
        public List<Modelo3dDto> Modelos { get; set; } = new();
        public long EspacoUsado { get; set; }
        public long EspacoTotal { get; set; }
        public long TamanhoMaximoArquivo { get; set; }
    }

    /// <summary>
    /// Uma peça nova ou alterada. Com X e Z, vale a posição livre do editor 3D; sem eles, a forma antiga (canto e lado em
    /// quadrados, como o mapa 2D põe a peça).
    /// </summary>
    public class ObjetoCenaFormDto
    {
        public int ModeloId { get; set; }
        [Range(0, 1000)] public int Coluna { get; set; }
        [Range(0, 1000)] public int Linha { get; set; }
        [Range(1, 10)] public int Tamanho { get; set; } = 1;
        /// <summary>Graus em volta do eixo vertical (qualquer valor: vira 0 a 360).</summary>
        public double Rotacao { get; set; }
        public bool Oculto { get; set; }

        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }
        public double? EscalaX { get; set; }
        public double? EscalaY { get; set; }
        public double? EscalaZ { get; set; }
        public double? GiroX { get; set; }
        public double? GiroZ { get; set; }
    }

    /// <summary>Limites do editor de cenário 3D, em quadrados.</summary>
    public static class ObjetoCenaRegras
    {
        public const double EscalaMinima = 0.1, EscalaMaxima = 40;
        public const double AlturaMinima = -10, AlturaMaxima = 50;
    }

    public class ObjetoCenaDto
    {
        public int ObjetoId { get; set; }
        public int CenaId { get; set; }
        public int ModeloId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Coluna { get; set; }
        public int Linha { get; set; }
        public int Tamanho { get; set; }
        public double Rotacao { get; set; }
        public bool Oculto { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double EscalaX { get; set; }
        public double EscalaY { get; set; }
        public double EscalaZ { get; set; }
        public double GiroX { get; set; }
        public double GiroZ { get; set; }

        public static ObjetoCenaDto De(ObjetoCena o) => new()
        {
            ObjetoId = o.ObjetoId, CenaId = o.CenaId, ModeloId = o.ModeloId, Nome = o.Modelo?.Nome ?? string.Empty,
            Coluna = o.Coluna, Linha = o.Linha, Tamanho = o.Tamanho, Rotacao = o.Rotacao, Oculto = o.Oculto,
            X = o.X, Y = o.Y, Z = o.Z, EscalaX = o.EscalaX, EscalaY = o.EscalaY, EscalaZ = o.EscalaZ, GiroX = o.GiroX, GiroZ = o.GiroZ,
        };
    }

    /// <summary>Renomear o modelo ou trocar a escala.</summary>
    public class Modelo3dEdicaoDto
    {
        [Required(ErrorMessage = "Dê um nome ao modelo."), StringLength(80, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Escala { get; set; } = Modelo3d.EscalaAuto;
    }

    public class MiniaturaDto
    {
        /// <summary>Modelo da categoria Miniatura, da mesma campanha; nulo tira a miniatura.</summary>
        public int? ModeloId { get; set; }
    }
}
