using System;
using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Anotacao
{
    public class PastaAnotacaoSaveDto
    {
        // Ignorado em atualizações: uma pasta não muda de campanha.
        public int CampanhaId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool IsPublica { get; set; }
    }

    public class PastaAnotacaoResponseDto
    {
        public int PastaId { get; set; }
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool IsPublica { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
