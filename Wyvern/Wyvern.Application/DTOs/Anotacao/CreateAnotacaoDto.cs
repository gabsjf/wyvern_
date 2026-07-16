using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Anotacao
{
    public class CreateAnotacaoDto
    {
        [Required]
        public int CampanhaId { get; set; }

        public int? PastaId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string Conteudo { get; set; } = string.Empty;
        
        public bool IsPublica { get; set; }
    }
}
