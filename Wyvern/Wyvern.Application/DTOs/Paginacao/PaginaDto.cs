using System.Collections.Generic;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.DTOs.Sessao;

namespace Wyvern.Application.DTOs.Paginacao
{
    /// <summary>Uma página de uma lista. <see cref="Pagina"/> começa em 1.</summary>
    public class PaginaDto<T>
    {
        public List<T> Itens { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    /// <summary>Quantos itens da lista são de cada campanha: os filtros da tela.</summary>
    public class ContagemPorCampanhaDto
    {
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    /// <summary>Campanha na estante: papel do usuário e os números da capa já contados.</summary>
    public class CampanhaNaListaDto
    {
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Sistema { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string MestreNome { get; set; } = string.Empty;
        public string Papel { get; set; } = string.Empty;
        public int TotalSessoes { get; set; }
        public int TotalHerois { get; set; }
    }

    public class PaginaDePersonagensDto : PaginaDto<PersonagemResponseDto>
    {
        /// <summary>Contagens das abas: valem para tudo o que o usuário vê, sem busca nem filtro.</summary>
        public int TotalHerois { get; set; }
        public int TotalMonstros { get; set; }
        /// <summary>Por campanha, na aba pedida, sem busca nem filtro de campanha.</summary>
        public List<ContagemPorCampanhaDto> PorCampanha { get; set; } = new();
    }

    public class PaginaDeSessoesDto : PaginaDto<SessaoResponseDto>
    {
        public List<ContagemPorCampanhaDto> PorCampanha { get; set; } = new();
    }
}
