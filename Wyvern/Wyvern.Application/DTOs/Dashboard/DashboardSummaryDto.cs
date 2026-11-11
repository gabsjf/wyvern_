using System;
using System.Collections.Generic;
using Wyvern.Application.DTOs.Paginacao;

namespace Wyvern.Application.DTOs.Dashboard
{
    /// <summary>Tudo o que o painel inicial mostra, numa resposta só e contado no banco.</summary>
    public class DashboardSummaryDto
    {
        public int TotalCampanhas { get; set; }
        public int TotalSessoes { get; set; }
        public int TotalHerois { get; set; }
        public int TotalNpcs { get; set; }
        /// <summary>Monstros (o nome antigo ficou para não quebrar quem já lê o campo).</summary>
        public int TotalViloes { get; set; }

        /// <summary>A campanha mais recente, para "Continue de onde parou". Nula sem campanhas.</summary>
        public CampanhaNaListaDto? Destaque { get; set; }
        /// <summary>A sessão de número mais alto da campanha em destaque.</summary>
        public SessaoNoPainelDto? UltimaSessaoDoDestaque { get; set; }

        /// <summary>As campanhas mais recentes (até 6); a lista inteira fica em /Campanha/pagina.</summary>
        public List<CampanhaNaListaDto> UltimasCampanhas { get; set; } = new();
        /// <summary>Agendadas de hoje em diante, a mais próxima primeiro (até 5).</summary>
        public List<SessaoNoPainelDto> ProximasSessoes { get; set; } = new();
        /// <summary>As últimas jogadas, a mais nova primeiro (até 5): o painel usa quando não há nada agendado.</summary>
        public List<SessaoNoPainelDto> SessoesRecentes { get; set; } = new();
        /// <summary>Os heróis criados mais recentemente (até 8).</summary>
        public List<HeroiNoPainelDto> Herois { get; set; } = new();
    }

    public class SessaoNoPainelDto
    {
        public int SessaoId { get; set; }
        public int NumeroSessao { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataSessao { get; set; }
        public DateTime? DataAgendada { get; set; }
        public int CampanhaId { get; set; }
        public string CampanhaNome { get; set; } = string.Empty;
    }

    public class HeroiNoPainelDto
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int CampanhaId { get; set; }
        public string CampanhaNome { get; set; } = string.Empty;
    }
}
