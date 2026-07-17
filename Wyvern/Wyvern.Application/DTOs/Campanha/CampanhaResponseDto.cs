using System;
using System.Collections.Generic;
using System.Text;

namespace Wyvern.Application.DTOs.Campanha
{
    public class CampanhaResponseDto
    {
        public int CampanhaId { get; set; }
        public string Nome { get; set; }
        public string Sistema { get; set; }
        public string? TokenConvite { get; set; }
        public string? Papel { get; set; } // "Mestre" ou "Jogador"

    }
}
