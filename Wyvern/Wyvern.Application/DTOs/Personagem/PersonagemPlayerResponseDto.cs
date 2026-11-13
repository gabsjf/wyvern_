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
        public string? Antecedente { get; set; }
        public string? Subclasse { get; set; }
        public string? Tamanho { get; set; }
        /// <summary>Nível liberado pelo Mestre por marco (0 = nenhum).</summary>
        public int NivelLiberado { get; set; }
        /// <summary>Até que nível o personagem pode subir agora (maior entre o XP e o marco). Calculado.</summary>
        public int NivelDisponivel { get; set; }
    }
}
