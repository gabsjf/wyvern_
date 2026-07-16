using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemConjuracao
    {
        [Key]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        public string? AtributoConjuracao { get; set; } // INT, SAB, CAR, etc.

        // Espaços de Magia (Spell Slots) Máximos
        public int SlotsNivel1Max { get; set; }
        public int SlotsNivel2Max { get; set; }
        public int SlotsNivel3Max { get; set; }
        public int SlotsNivel4Max { get; set; }
        public int SlotsNivel5Max { get; set; }
        public int SlotsNivel6Max { get; set; }
        public int SlotsNivel7Max { get; set; }
        public int SlotsNivel8Max { get; set; }
        public int SlotsNivel9Max { get; set; }

        // Espaços de Magia (Spell Slots) Atuais (não gastos)
        public int SlotsNivel1Atual { get; set; }
        public int SlotsNivel2Atual { get; set; }
        public int SlotsNivel3Atual { get; set; }
        public int SlotsNivel4Atual { get; set; }
        public int SlotsNivel5Atual { get; set; }
        public int SlotsNivel6Atual { get; set; }
        public int SlotsNivel7Atual { get; set; }
        public int SlotsNivel8Atual { get; set; }
        public int SlotsNivel9Atual { get; set; }
    }
}
