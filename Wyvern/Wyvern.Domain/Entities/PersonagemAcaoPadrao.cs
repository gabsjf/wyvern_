using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemAcaoPadrao
    {
        [Key]
        public int PersonagemAcaoPadraoId { get; set; }

        [Required]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; }

        public string? Descricao { get; set; }
        public string? Alcance { get; set; }
        public int BonusAcerto { get; set; }
        public string? Dano { get; set; }
        public string? TipoDano { get; set; }
        public string? Propriedades { get; set; }

        public string? AtributoBase { get; set; }
        public bool Proficiente { get; set; }
    }
}
