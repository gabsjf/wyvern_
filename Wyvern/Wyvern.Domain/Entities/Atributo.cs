using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class Atributo
    {
        [Key]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        [Required]
        public int Forca { get; set; }
        [Required]
        public int Destreza { get; set; }
        [Required]
        public int Constituicao { get; set; }
        [Required]
        public int Inteligencia { get; set; }
        [Required]
        public int Sabedoria { get; set; }
        [Required]
        public int Carisma { get; set; }

        public bool ProficienciaSalvaguardaForca { get; set; }
        public bool ProficienciaSalvaguardaDestreza { get; set; }
        public bool ProficienciaSalvaguardaConstituicao { get; set; }
        public bool ProficienciaSalvaguardaInteligencia { get; set; }
        public bool ProficienciaSalvaguardaSabedoria { get; set; }
        public bool ProficienciaSalvaguardaCarisma { get; set; }

        public Personagem Personagem { get; set; }
    }
}