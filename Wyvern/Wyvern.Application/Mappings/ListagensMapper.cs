using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Wyvern.Application.DTOs.Dashboard;
using Wyvern.Application.DTOs.Paginacao;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.DTOs.Sessao;
using Wyvern.Domain.Consultas;

namespace Wyvern.Application.Mappings
{
    /// <summary>Converte as consultas de listagem (Domain) nas respostas da API.</summary>
    public static class ListagensMapper
    {
        public static PaginaDto<TDto> ParaDto<T, TDto>(Pagina<T> pagina, IEnumerable<TDto> itens) => new()
        {
            Itens = itens.ToList(),
            Total = pagina.Total,
            Pagina = pagina.Numero,
            TamanhoPagina = pagina.Tamanho,
            TotalPaginas = pagina.TotalPaginas,
        };

        public static PaginaDto<CampanhaNaListaDto> ParaDto(Pagina<CampanhaNaLista> pagina) =>
            ParaDto(pagina, pagina.Itens.Select(c => ParaDto(c)));

        public static PaginaDePersonagensDto ParaDto(PaginaDePersonagens resultado, IMapper mapper)
        {
            var p = resultado.Pagina;
            return new PaginaDePersonagensDto
            {
                Itens = mapper.Map<List<PersonagemResponseDto>>(p.Itens),
                Total = p.Total,
                Pagina = p.Numero,
                TamanhoPagina = p.Tamanho,
                TotalPaginas = p.TotalPaginas,
                TotalHerois = resultado.TotalHerois,
                TotalMonstros = resultado.TotalMonstros,
                PorCampanha = resultado.PorCampanha.Select(c => ParaDto(c)).ToList(),
            };
        }

        public static PaginaDeSessoesDto ParaDto(PaginaDeSessoes resultado, IMapper mapper)
        {
            var p = resultado.Pagina;
            return new PaginaDeSessoesDto
            {
                Itens = mapper.Map<List<SessaoResponseDto>>(p.Itens),
                Total = p.Total,
                Pagina = p.Numero,
                TamanhoPagina = p.Tamanho,
                TotalPaginas = p.TotalPaginas,
                PorCampanha = resultado.PorCampanha.Select(c => ParaDto(c)).ToList(),
            };
        }

        public static DashboardSummaryDto ParaDto(ResumoDoPainel r) => new()
        {
            TotalCampanhas = r.TotalCampanhas,
            TotalSessoes = r.TotalSessoes,
            TotalHerois = r.TotalHerois,
            TotalNpcs = r.TotalNpcs,
            TotalViloes = r.TotalMonstros,
            Destaque = r.Destaque == null ? null : ParaDto(r.Destaque),
            UltimaSessaoDoDestaque = r.UltimaSessaoDoDestaque == null ? null : ParaDto(r.UltimaSessaoDoDestaque),
            UltimasCampanhas = r.Campanhas.Select(c => ParaDto(c)).ToList(),
            ProximasSessoes = r.ProximasSessoes.Select(s => ParaDto(s)).ToList(),
            SessoesRecentes = r.SessoesRecentes.Select(s => ParaDto(s)).ToList(),
            Herois = r.Herois.Select(h => new HeroiNoPainelDto
            {
                PersonagemId = h.PersonagemId, Nome = h.Nome, CampanhaId = h.CampanhaId, CampanhaNome = h.CampanhaNome,
            }).ToList(),
        };

        private static CampanhaNaListaDto ParaDto(CampanhaNaLista c) => new()
        {
            CampanhaId = c.CampanhaId,
            Nome = c.Nome,
            Sistema = c.Sistema ?? string.Empty,
            Ativo = c.Ativo,
            MestreNome = c.MestreNome,
            Papel = c.Papel.ToString(),
            TotalSessoes = c.TotalSessoes,
            TotalHerois = c.TotalHerois,
        };

        private static SessaoNoPainelDto ParaDto(SessaoNoPainel s) => new()
        {
            SessaoId = s.SessaoId,
            NumeroSessao = s.NumeroSessao,
            Nome = s.Nome,
            DataSessao = s.DataSessao,
            DataAgendada = s.DataAgendada,
            CampanhaId = s.CampanhaId,
            CampanhaNome = s.CampanhaNome,
        };

        private static ContagemPorCampanhaDto ParaDto(ContagemPorCampanha c) => new()
        {
            CampanhaId = c.CampanhaId, Nome = c.Nome, Total = c.Total,
        };
    }
}
