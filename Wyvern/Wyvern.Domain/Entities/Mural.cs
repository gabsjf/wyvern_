using System.ComponentModel.DataAnnotations;

namespace Wyvern.Domain.Entities
{
    /// <summary>
    /// Um quadro livre do Mestre (estilo Miro): notas, textos, formas, setas, imagens e cartões da campanha.
    /// O conteúdo é um documento JSON montado pelo front; o servidor só guarda, confere o tamanho e controla a versão.
    /// Só o Mestre da campanha vê e edita.
    /// </summary>
    public class Mural
    {
        public int MuralId { get; set; }

        public int CampanhaId { get; set; }
        public Campanha? Campanha { get; set; }

        [Required, StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Elementos e conectores, em JSON.</summary>
        public string Conteudo { get; set; } = "{}";

        /// <summary>Sobe a cada gravação: quem salva com uma versão antiga recebe conflito em vez de apagar a mudança de outro.</summary>
        public int Versao { get; set; } = 1;

        public int? CriadoPorId { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    /// <summary>Imagem colocada num mural, em tabela própria para a lista e o documento não carregarem os bytes.</summary>
    public class MuralImagem
    {
        public int MuralImagemId { get; set; }

        public int MuralId { get; set; }
        public Mural? Mural { get; set; }

        [Required, StringLength(20)]
        public string Tipo { get; set; } = "image/png";

        public byte[] Dados { get; set; } = Array.Empty<byte>();

        public DateTime CriadoEm { get; set; }
    }
}
