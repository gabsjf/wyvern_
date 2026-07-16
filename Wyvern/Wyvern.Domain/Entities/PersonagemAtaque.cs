using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemAtaque
    {
        [Key]
        public int PersonagemAtaqueId { get; set; }

        [Required]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; } // ex: Espada Longa

        public string Alcance { get; set; } // ex: 1,5m

        public int BonusAcerto { get; set; } // ex: +5

        public string Dano { get; set; } // ex: 1d8+3

        public string TipoDano { get; set; } // ex: Cortante

        public string Propriedades { get; set; } // ex: Versátil (1d10)

        public string? AtributoBase { get; set; } // FOR, DES, INT, SAB, CAR...
        public bool Proficiente { get; set; }
    }
}
