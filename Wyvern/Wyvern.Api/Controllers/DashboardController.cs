using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Dashboard;
using Wyvern.Domain.Interfaces.Repositories;
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
        private readonly Wyvern.Domain.Interfaces.ICurrentUserService _currentUser;

        public DashboardController(IUnitOfWork uof, IMapper mapper, Wyvern.Domain.Interfaces.ICurrentUserService currentUser)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        [HttpGet("resumo")]
        public async Task<ActionResult<DashboardSummaryDto>> GetResumo()
        {
            var campanhas = await _uof.CampanhaRepository.GetCampanhasAsync();
            var sessoes = await _uof.SessaoRepository.GetSessoesAsync();
            var personagens = await _uof.PersonagemRepository.GetPersonagensAsync();

            // Papel do usuário em cada campanha exibida (as campanhas já vêm com os membros carregados)
            var ultimasCampanhas = campanhas.OrderByDescending(c => c.CampanhaId).Take(4).ToList();
            var ultimasCampanhasDto = _mapper.Map<List<CampanhaResponseDto>>(ultimasCampanhas);
            for (var i = 0; i < ultimasCampanhasDto.Count; i++)
            {
                ultimasCampanhasDto[i].Papel = ultimasCampanhas[i].Membros?
                    .FirstOrDefault(m => m.UsuarioId == _currentUser.UserId)?.Papel.ToString();
            }

            var resumo = new DashboardSummaryDto
            {
                TotalCampanhas = campanhas.Count(),
                TotalHerois = personagens.Count(p => p.TipoId == 1),
                TotalNpcs = personagens.Count(p => p.TipoId == 2),
                TotalViloes = personagens.Count(p => p.TipoId == 3),
                
                UltimasCampanhas = ultimasCampanhasDto,
                
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
