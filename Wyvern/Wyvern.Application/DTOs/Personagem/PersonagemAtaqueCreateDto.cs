using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemAtaqueCreateDto
    {
        [Required]
        public string Nome { get; set; } 
        public string Alcance { get; set; } 
        public int BonusAcerto { get; set; } 
        public string Dano { get; set; } 
        public string TipoDano { get; set; } 
        public string Propriedades { get; set; }
        public string? AtributoBase { get; set; }
        public bool Proficiente { get; set; }
    }
}
