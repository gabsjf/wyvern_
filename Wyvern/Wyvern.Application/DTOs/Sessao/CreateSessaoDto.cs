using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Campanha;

namespace Wyvern.Application.DTOs.Sessao
{
    public class CreateSessaoDto
    {
        public int NumeroSessao { get; set; }
        public string Nome { get; set; }
        public DateTime DataSessao { get; set; }
        public DateTime? DataAgendada { get; set; }
        public string Obs { get; set; }
        public int CampanhaId { get; set; }
    }
}
