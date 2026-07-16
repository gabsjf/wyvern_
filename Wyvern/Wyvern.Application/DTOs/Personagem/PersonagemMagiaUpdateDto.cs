using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemMagiaUpdateDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int MagiaIdAtual { get; set; }
        [Required]
        public int NovaMagiaId { get; set; }
    }
}
