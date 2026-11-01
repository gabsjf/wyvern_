using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Anotacao;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PastaAnotacaoController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ICurrentUserService _currentUser;
        private readonly ICampanhaAuthorizationService _campanhaAuth;

        public PastaAnotacaoController(IUnitOfWork uof, ICurrentUserService currentUser, ICampanhaAuthorizationService campanhaAuth)
        {
            _uof = uof;
            _currentUser = currentUser;
            _campanhaAuth = campanhaAuth;
        }

        private static PastaAnotacaoResponseDto ParaDto(PastaAnotacao p) => new()
        {
            PastaId = p.PastaId,
            CampanhaId = p.CampanhaId,
            Nome = p.Nome,
            IsPublica = p.IsPublica,
            CriadoEm = p.CriadoEm
        };

        private ActionResult NaoParticipa() =>
            Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Você não participa desta campanha.");

        [HttpGet("campanha/{campanhaId}")]
        public async Task<ActionResult<IEnumerable<PastaAnotacaoResponseDto>>> GetPastas(int campanhaId)
        {
            if (!await _campanhaAuth.IsMembroAsync(campanhaId, _currentUser.UserId)) return NaoParticipa();

            var pastas = await _uof.PastaAnotacaoRepository.GetPastasByCampanhaAsync(campanhaId);
            return Ok(pastas.Select(ParaDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PastaAnotacaoResponseDto>> GetPastaById(int id)
        {
            var p = await _uof.PastaAnotacaoRepository.GetPastaAsync(id);
            if (p == null) return NotFound("Pasta não encontrada");
            if (!await _campanhaAuth.IsMembroAsync(p.CampanhaId, _currentUser.UserId)) return NaoParticipa();

            return Ok(ParaDto(p));
        }

        [HttpPost]
        public async Task<ActionResult<PastaAnotacaoResponseDto>> CreatePasta([FromBody] PastaAnotacaoSaveDto dto)
        {
            if (!await _campanhaAuth.IsMembroAsync(dto.CampanhaId, _currentUser.UserId)) return NaoParticipa();

            var isMestre = await _campanhaAuth.IsMestreAsync(dto.CampanhaId, _currentUser.UserId);

            var novaPasta = new PastaAnotacao
            {
                CampanhaId = dto.CampanhaId,
                Nome = dto.Nome,
                // Como nas anotações, só o Mestre publica conteúdo para a mesa.
                IsPublica = isMestre && dto.IsPublica,
                CriadoEm = System.DateTime.Now
            };

            await _uof.PastaAnotacaoRepository.CreatePastaAsync(novaPasta);
            await _uof.CommitAsync();
            return CreatedAtAction(nameof(GetPastaById), new { id = novaPasta.PastaId }, ParaDto(novaPasta));
        }

        // Pastas ainda não guardam o autor, então só o Mestre da campanha edita ou exclui.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePasta(int id, [FromBody] PastaAnotacaoSaveDto dto)
        {
            var pasta = await _uof.PastaAnotacaoRepository.GetPastaAsync(id);
            if (pasta == null) return NotFound("Pasta não encontrada");
            if (!await _campanhaAuth.IsMestreAsync(pasta.CampanhaId, _currentUser.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode editar pastas.");

            pasta.Nome = dto.Nome;
            pasta.IsPublica = dto.IsPublica;

            await _uof.PastaAnotacaoRepository.UpdatePastaAsync(pasta);
            await _uof.CommitAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasta(int id)
        {
            var pasta = await _uof.PastaAnotacaoRepository.GetPastaAsync(id);
            if (pasta == null) return NotFound("Pasta não encontrada");
            if (!await _campanhaAuth.IsMestreAsync(pasta.CampanhaId, _currentUser.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode excluir pastas.");

            await _uof.PastaAnotacaoRepository.DeletePastaAsync(id);
            await _uof.CommitAsync();
            return NoContent();
        }
    }
}
