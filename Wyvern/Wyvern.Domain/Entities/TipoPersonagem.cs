using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class TipoPersonagem
    {
        [Key]
        public int Id { get; set; }
        public String Nome { get; set; }
    }
}
