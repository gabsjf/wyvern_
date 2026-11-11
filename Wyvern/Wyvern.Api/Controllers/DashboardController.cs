using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Dashboard;
using Wyvern.Application.Mappings;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IUnitOfWork _uof;

        public DashboardController(IUnitOfWork uof) => _uof = uof;

        /// <summary>
        /// O painel inteiro numa resposta: totais, campanha em destaque com a última sessão, campanhas recentes,
        /// sessões agendadas e recentes e os heróis mais novos. Tudo contado e cortado no banco.
        /// </summary>
        [HttpGet("resumo")]
        public async Task<ActionResult<DashboardSummaryDto>> GetResumo()
        {
            var resumo = await _uof.ListagensRepository.PainelAsync(DateTime.Now.Date);
            return Ok(ListagensMapper.ParaDto(resumo));
        }
    }
}
