using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    /// <summary>Para qual campanha o jogador traz uma cópia do próprio herói.</summary>
    public class PersonagemTrazerDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Informe a campanha.")]
        public int CampanhaId { get; set; }
    }
}
