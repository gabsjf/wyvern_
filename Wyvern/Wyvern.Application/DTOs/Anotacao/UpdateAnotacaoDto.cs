using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Anotacao
{
    public class UpdateAnotacaoDto
    {
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Conteudo { get; set; }
        
        public bool IsPublica { get; set; }
        
        public int? PastaId { get; set; }
    }
}
