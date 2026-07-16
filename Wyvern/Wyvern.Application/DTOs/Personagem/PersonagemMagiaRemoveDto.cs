using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemMagiaRemoveDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int MagiaId { get; set; }
    }
}
