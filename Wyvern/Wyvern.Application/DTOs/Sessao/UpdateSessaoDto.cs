using System;
using System.Collections.Generic;
using System.Text;

namespace Wyvern.Application.DTOs.Sessao
{
    public class UpdateSessaoDto
    {
        public int NumeroSessao { get; set; }
        public string Nome { get; set; }
        public DateTime DataSessao { get; set; }
        public DateTime? DataAgendada { get; set; }
        public string Obs { get; set; }
        public int CampanhaId { get; set; }
    }
}
