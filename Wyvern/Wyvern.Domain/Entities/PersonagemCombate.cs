using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class PersonagemCombate
    {
        [Key]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }
        public int VidaAtual { get; set; }
        public int VidaMaxima { get; set; }
        public int ClasseArmadura { get; set; }
        public int Iniciativa { get; set; }
        public string? Deslocamento { get; set; } // Agora string para suportar '9m', etc.
        public int ProficienciaBonus { get; set; } = 2; // Padrão de D&D nv 1
        public bool InspiracaoHeroica { get; set; }
        public int VidaTemporaria { get; set; }
        public string? DadoVidaMaximo { get; set; }
        public int DadoVidaGasto { get; set; }
        public int DeathSaveSucessos { get; set; }
        public int DeathSaveFalhas { get; set; }
        public int ClasseArmaduraEscudo { get; set; }
    }
}
