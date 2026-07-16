using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class PersonagemPericia
    {
        [Key]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }
        public int PericiaId { get; set; }
        public Pericia Pericia { get; set; }
        public int Bonus { get; set; }
        public bool Proficiente { get; set; }
    }
}
