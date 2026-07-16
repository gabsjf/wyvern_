using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemPericiaUpdateDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int PericiaId { get; set; }
        public int Bonus { get; set; }
        public bool Proficiente { get; set; }
    }
}
