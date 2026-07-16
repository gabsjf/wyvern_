using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Wyvern.Application.DTOs.Atributo
{
    public class CreateAtributoDto
    {
        public int PersonagemId { get; set; }
        
        public int Forca { get; set; }
       
        public int Destreza { get; set; }
        
        public int Constituicao { get; set; }
        
        public int Inteligencia { get; set; }
        
        public int Sabedoria { get; set; }
        
        public int Carisma { get; set; }
    }
}
