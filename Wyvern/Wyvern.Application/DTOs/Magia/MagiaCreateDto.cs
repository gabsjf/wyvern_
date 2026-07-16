using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Application.DTOs.Magia
{
    public class MagiaCreateDto
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public int Nivel { get; set; }
    }
}
