using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemPlayerUpdateDto
    {
        [Required]
        public int PersonagemId { get; set; }
        [Required]
        [StringLength(50)]
        public string Classe { get; set; } = string.Empty;
        [Required]
        public string Raca { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public int Xp { get; set; }
        public string Alinhamento { get; set; } = string.Empty;
    }
}
