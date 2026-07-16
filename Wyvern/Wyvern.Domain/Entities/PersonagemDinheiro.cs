using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemDinheiro
    {
        [Key]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        public int PC { get; set; }
        public int PP { get; set; }
        public int PE { get; set; }
        public int PO { get; set; }
        public int PL { get; set; }
    }
}
