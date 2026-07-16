using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemTracoEspecial
    {
        [Key]
        public int PersonagemTracoEspecialId { get; set; }

        [Required]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; }

        [Required]
        public string Descricao { get; set; }
    }
}
