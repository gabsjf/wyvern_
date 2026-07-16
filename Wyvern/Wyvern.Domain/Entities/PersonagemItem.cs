using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class PersonagemItem
    {
        [Key]
        public int PersonagemItemId { get; set; }
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        [Required]
        public string Nome { get; set; }
        public string TipoItem { get; set; }
        public string Raridade { get; set; }
        public string Descricao { get; set; }
    }
}
