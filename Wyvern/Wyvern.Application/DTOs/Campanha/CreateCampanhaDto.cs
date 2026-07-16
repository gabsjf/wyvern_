using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Wyvern.Application.DTOs.Sessao;

namespace Wyvern.Application.DTOs.Campanha
{
    public class CreateCampanhaDto
    {
        [Required]
        public string Nome { get; set; }

        [Required]
        public string Sistema { get; set; }

        public int MestreId { get; set; }
    }
}
