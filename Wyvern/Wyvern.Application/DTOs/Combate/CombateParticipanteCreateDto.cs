using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Combate
{
    public class CombateParticipanteCreateDto
    {
        public int? PersonagemId { get; set; }
        public string? NomeNPC { get; set; }
        public int Iniciativa { get; set; }
        public int VidaAtual { get; set; }
        public int VidaMaxima { get; set; }
        public int ClasseArmadura { get; set; }
        public bool IsInimigo { get; set; }
        public string Condicoes { get; set; } = string.Empty;
    }
}
