using System;
using System.Collections.Generic;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.DTOs.Sessao;

namespace Wyvern.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalCampanhas { get; set; }
        public int TotalHerois { get; set; }
        public int TotalNpcs { get; set; }
        public int TotalViloes { get; set; }
        public List<CampanhaResponseDto> UltimasCampanhas { get; set; } = new List<CampanhaResponseDto>();
        public List<SessaoResponseDto> ProximasSessoes { get; set; } = new List<SessaoResponseDto>();
    }
}
