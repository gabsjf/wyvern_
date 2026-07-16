using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemPericiaRemoveDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int PericiaId { get; set; }
    }
}
