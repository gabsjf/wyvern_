using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemRemoveItemDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        public int ItemId { get; set; }
    }
}
