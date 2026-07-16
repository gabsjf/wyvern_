using System;
using System.Collections.Generic;
using System.Text;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemPlayerResponseDto
    {
        public int PersonagemId { get; set; }
        public string Classe { get; set; } = string.Empty;
        public string Raca { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public int Xp { get; set; }
        public string Alinhamento { get; set; } = string.Empty;
    }
}
