using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemAcaoLendaria
    {
        [Key]
        public int PersonagemAcaoLendariaId { get; set; }

        [Required]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; }

        public string? CustoAcao { get; set; }

        [Required]
        public string Descricao { get; set; }
    }
}
