using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class Pericia
    {
        // proficiencia,modificador de atributo estão na entidade de relacionamento de pericia e personagem
        [Key]
        public int PericiaId { get; set; }
        [Required]
        public string Nome { get; set; }
        public bool Ativo { get; set; } = true; 
    }
}
