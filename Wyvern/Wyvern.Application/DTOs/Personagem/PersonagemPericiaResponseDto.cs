using Wyvern.Application.DTOs.Pericia;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemPericiaResponseDto
    {
        public int PersonagemId { get; set; }
        public int PericiaId { get; set; }
        public PericiaResponseDto? Pericia { get; set; }
    }
}
