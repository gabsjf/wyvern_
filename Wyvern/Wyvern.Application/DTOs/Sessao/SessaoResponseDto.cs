using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Wyvern.Application.DTOs.Campanha;

namespace Wyvern.Application.DTOs.Sessao
{
    public class SessaoResponseDto
    {
        public int SessaoId { get; set; }
        public int NumeroSessao { get; set; }
        public string Nome { get; set; }
        public DateTime DataSessao { get; set; }
        public DateTime? DataAgendada { get; set; }
        public string Obs { get; set; }
        public int CampanhaId { get; set; }
        public CampanhaResponseDto? Campanha { get; set; }
        public bool Ativo { get; set; } = true;

    }
}
