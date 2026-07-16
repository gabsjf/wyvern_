using System;
using System.Collections.Generic;
using System.Text;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemCombateResponseDto
    {
        public int PersonagemId { get; set; }
        public int VidaAtual { get; set; }
        public int VidaMaxima { get; set; }
        public int ClasseArmadura { get; set; }
        public int Iniciativa { get; set; }
        public string? Deslocamento { get; set; }
        public int ProficienciaBonus { get; set; }
        public bool InspiracaoHeroica { get; set; }
        public int VidaTemporaria { get; set; }
        public string? DadoVidaMaximo { get; set; }
        public int DadoVidaGasto { get; set; }
        public int DeathSaveSucessos { get; set; }
        public int DeathSaveFalhas { get; set; }
        public int ClasseArmaduraEscudo { get; set; }
    }
}
