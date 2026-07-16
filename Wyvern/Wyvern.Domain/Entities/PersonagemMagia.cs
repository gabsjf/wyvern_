using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class PersonagemMagia
    {
        [Key]
        public int PersonagemMagiaId { get; set; }
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; }
        public int Nivel { get; set; } // Truque (0) até 9
        public string Escola { get; set; }
        
        public bool Verbal { get; set; }
        public bool Somatico { get; set; }
        public bool Material { get; set; }
        
        public string Descricao { get; set; }
    }
}
