using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Dashboard;
using Wyvern.Infrastructure.Repositories;
using AutoMapper;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Application.DTOs.Sessao;
using System.Collections.Generic;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;

        public DashboardController(IUnitOfWork uof, IMapper mapper)
        {
            _uof = uof;
            _mapper = mapper;
        }

        [HttpGet("resumo")]
        public async Task<ActionResult<DashboardSummaryDto>> GetResumo()
        {
            var campanhas = await _uof.CampanhaRepository.GetCampanhasAsync();
            var sessoes = await _uof.SessaoRepository.GetSessoesAsync();
            var personagens = await _uof.PersonagemRepository.GetPersonagensAsync();

            var resumo = new DashboardSummaryDto
            {
                TotalCampanhas = campanhas.Count(),
                TotalHerois = personagens.Count(p => p.TipoId == 1),
                TotalNpcs = personagens.Count(p => p.TipoId == 2),
                TotalViloes = personagens.Count(p => p.TipoId == 3),
                
                UltimasCampanhas = _mapper.Map<List<CampanhaResponseDto>>(
                    campanhas.OrderByDescending(c => c.CampanhaId).Take(4)
                ),
                
                ProximasSessoes = _mapper.Map<List<SessaoResponseDto>>(
                    sessoes.Where(s => s.DataAgendada.HasValue && s.DataAgendada.Value >= System.DateTime.Now.Date)
                           .OrderBy(s => s.DataAgendada)
                           .Take(5)
                )
            };

            return Ok(resumo);
        }
    }
}
