using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Wyvern.Domain.Entities
{
    public class Item
    {
        [Key]
        public int ItemId { get; set; }
        [Required]
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
