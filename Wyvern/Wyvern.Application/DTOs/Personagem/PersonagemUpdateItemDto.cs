using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemUpdateItemDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int ItemIdAtual { get; set; }
        [Required]
        public int NovoItemId { get; set; }
    }
}
