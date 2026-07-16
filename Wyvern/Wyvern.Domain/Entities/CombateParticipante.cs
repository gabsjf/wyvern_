using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    public class CombateParticipante
    {
        [Key]
        public int ParticipanteId { get; set; }
        public int CombateId { get; set; }
        [JsonIgnore]
        public Combate? Combate { get; set; }
        public int? PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }
        
        public string? NomeNPC { get; set; }
        public int Iniciativa { get; set; }
        public int VidaAtual { get; set; }
        public int VidaMaxima { get; set; }
        public int ClasseArmadura { get; set; }
        public bool IsInimigo { get; set; } = false;
        public string Condicoes { get; set; } = string.Empty;
        
        public int SucessosMorte { get; set; } = 0;
        public int FalhasMorte { get; set; } = 0;
    }
}
