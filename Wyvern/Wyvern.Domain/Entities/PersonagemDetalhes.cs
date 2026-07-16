using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemDetalhes
    {
        [Key]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        public string? Aparencia { get; set; }
        public string? HistoriaPersonalidade { get; set; }
        public string? TracosEspecie { get; set; }
        public string? Talentos { get; set; }
        public string? CaracteristicasClasse { get; set; }
        public string? ProficienciaArmas { get; set; }
        public string? ProficienciaFerramentas { get; set; }
        public string? Idiomas { get; set; }

        public bool ProficienciaArmaduraLeve { get; set; }
        public bool ProficienciaArmaduraMedia { get; set; }
        public bool ProficienciaArmaduraPesada { get; set; }
        public bool ProficienciaEscudos { get; set; }
    }
}
