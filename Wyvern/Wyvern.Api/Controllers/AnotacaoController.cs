using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Anotacao;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Wyvern.Domain.Interfaces;
using Wyvern.Application.Services;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AnotacaoController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ICurrentUserService _currentUser;
        private readonly ICampanhaAuthorizationService _campanhaAuth;

        public AnotacaoController(IUnitOfWork uof, ICurrentUserService currentUser, ICampanhaAuthorizationService campanhaAuth)
        {
            _uof = uof;
            _currentUser = currentUser;
            _campanhaAuth = campanhaAuth;
        }

        [HttpGet("campanha/{campanhaId}")]
        public async Task<ActionResult<IEnumerable<AnotacaoResponseDto>>> GetAnotacoes(int campanhaId)
        {
            bool isMestre = await _campanhaAuth.IsMestreAsync(campanhaId, _currentUser.UserId);

            var anotacoes = await _uof.AnotacaoRepository.GetAnotacoesByCampanhaAsync(campanhaId);
            
            if (!isMestre)
            {
                anotacoes = anotacoes.Where(a => a.IsPublica || a.CriadoPorId == _currentUser.UserId);
            }
            else
            {
                // Mestre não vê as notas dos jogadores
                anotacoes = anotacoes.Where(a => a.IsPublica || a.CriadoPorId == _currentUser.UserId);
            }

            var result = anotacoes.Select(a => new AnotacaoResponseDto
            {
                AnotacaoId = a.AnotacaoId,
                CampanhaId = a.CampanhaId,
                PastaId = a.PastaId,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                IsPublica = a.IsPublica,
                CriadoEm = a.CriadoEm,
                CriadoPorId = a.CriadoPorId
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnotacaoResponseDto>> GetAnotacaoById(int id)
        {
            var a = await _uof.AnotacaoRepository.GetAnotacaoAsync(id);
            if (a == null) return NotFound("Anotação não encontrada");

            bool isMestre = await _campanhaAuth.IsMestreAsync(a.CampanhaId, _currentUser.UserId);

            if (!isMestre && !a.IsPublica && a.CriadoPorId != _currentUser.UserId)
            {
                return Forbid("Você não tem acesso a esta anotação.");
            }

            var dto = new AnotacaoResponseDto
            {
                AnotacaoId = a.AnotacaoId,
                CampanhaId = a.CampanhaId,
                PastaId = a.PastaId,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                IsPublica = a.IsPublica,
                CriadoEm = a.CriadoEm,
                CriadoPorId = a.CriadoPorId
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AnotacaoResponseDto>> CreateAnotacao([FromBody] CreateAnotacaoDto dto)
        {
            bool isMestre = await _campanhaAuth.IsMestreAsync(dto.CampanhaId, _currentUser.UserId);

            var novaAnotacao = new Wyvern.Domain.Entities.Anotacao
            {
                CampanhaId = dto.CampanhaId,
                PastaId = dto.PastaId,
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo ?? string.Empty,
                IsPublica = isMestre ? dto.IsPublica : false,
                CriadoPorId = _currentUser.UserId,
                CriadoEm = System.DateTime.Now
            };

            await _uof.AnotacaoRepository.CreateAnotacaoAsync(novaAnotacao);
            await _uof.CommitAsync();

            var responseDto = new AnotacaoResponseDto
            {
                AnotacaoId = novaAnotacao.AnotacaoId,
                CampanhaId = novaAnotacao.CampanhaId,
                PastaId = novaAnotacao.PastaId,
                Titulo = novaAnotacao.Titulo,
                Conteudo = novaAnotacao.Conteudo,
                IsPublica = novaAnotacao.IsPublica,
                CriadoEm = novaAnotacao.CriadoEm,
                CriadoPorId = novaAnotacao.CriadoPorId
            };

            return CreatedAtAction(nameof(GetAnotacaoById), new { id = novaAnotacao.AnotacaoId }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnotacao(int id, [FromBody] UpdateAnotacaoDto dto)
        {
            var anotacao = await _uof.AnotacaoRepository.GetAnotacaoAsync(id);
            if (anotacao == null) return NotFound("Anotação não encontrada");

            bool isMestre = await _campanhaAuth.IsMestreAsync(anotacao.CampanhaId, _currentUser.UserId);

            if (!isMestre && anotacao.CriadoPorId != _currentUser.UserId)
            {
                return Forbid("Apenas o autor ou o mestre podem editar esta anotação.");
            }

            // Apenas o mestre pode mudar a visibilidade pública de algo
            if (isMestre)
            {
                anotacao.IsPublica = dto.IsPublica;
            }

            anotacao.Titulo = dto.Titulo;
            anotacao.Conteudo = dto.Conteudo ?? string.Empty;
            anotacao.PastaId = dto.PastaId;

            await _uof.AnotacaoRepository.UpdateAnotacaoAsync(anotacao);
            await _uof.CommitAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnotacao(int id)
        {
            var anotacao = await _uof.AnotacaoRepository.GetAnotacaoAsync(id);
            if (anotacao == null) return NotFound("Anotação não encontrada");

            bool isMestre = await _campanhaAuth.IsMestreAsync(anotacao.CampanhaId, _currentUser.UserId);

            if (!isMestre && anotacao.CriadoPorId != _currentUser.UserId)
            {
                return Forbid("Apenas o autor ou o mestre podem deletar esta anotação.");
            }

            await _uof.AnotacaoRepository.DeleteAnotacaoAsync(id);
            await _uof.CommitAsync();
            return NoContent();
        }
    }
}
