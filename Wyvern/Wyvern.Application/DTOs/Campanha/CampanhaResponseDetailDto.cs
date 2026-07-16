using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Sessao;

namespace Wyvern.Application.DTOs.Campanha
{
    public class CampanhaResponseDetailDto
    {
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Sistema { get; set; } = string.Empty;
        public string MestreNome { get; set; } = string.Empty;

        public List<SessaoResponseDto> Sessoes { get; set; } = new();
    }
}
