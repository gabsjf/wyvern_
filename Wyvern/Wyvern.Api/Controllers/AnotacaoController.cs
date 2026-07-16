using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Anotacao;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AnotacaoController : ControllerBase
    {
        private readonly IUnitOfWork _uof;

        public AnotacaoController(IUnitOfWork uof)
        {
            _uof = uof;
        }

        [HttpGet("campanha/{campanhaId}")]
        public async Task<ActionResult<IEnumerable<AnotacaoResponseDto>>> GetAnotacoes(int campanhaId)
        {
            var anotacoes = await _uof.AnotacaoRepository.GetAnotacoesByCampanhaAsync(campanhaId);
            var result = anotacoes.Select(a => new AnotacaoResponseDto
            {
                AnotacaoId = a.AnotacaoId,
                CampanhaId = a.CampanhaId,
                PastaId = a.PastaId,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                IsPublica = a.IsPublica,
                CriadoEm = a.CriadoEm
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnotacaoResponseDto>> GetAnotacaoById(int id)
        {
            var a = await _uof.AnotacaoRepository.GetAnotacaoAsync(id);
            if (a == null) return NotFound("Anotação não encontrada");

            var dto = new AnotacaoResponseDto
            {
                AnotacaoId = a.AnotacaoId,
                CampanhaId = a.CampanhaId,
                PastaId = a.PastaId,
                Titulo = a.Titulo,
                Conteudo = a.Conteudo,
                IsPublica = a.IsPublica,
                CriadoEm = a.CriadoEm
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AnotacaoResponseDto>> CreateAnotacao([FromBody] CreateAnotacaoDto dto)
        {
            var novaAnotacao = new Wyvern.Domain.Entities.Anotacao
            {
                CampanhaId = dto.CampanhaId,
                PastaId = dto.PastaId,
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo ?? string.Empty,
                IsPublica = dto.IsPublica,
                CriadoEm = System.DateTime.Now
            };

            await _uof.AnotacaoRepository.CreateAnotacaoAsync(novaAnotacao);

            var responseDto = new AnotacaoResponseDto
            {
                AnotacaoId = novaAnotacao.AnotacaoId,
                CampanhaId = novaAnotacao.CampanhaId,
                PastaId = novaAnotacao.PastaId,
                Titulo = novaAnotacao.Titulo,
                Conteudo = novaAnotacao.Conteudo,
                IsPublica = novaAnotacao.IsPublica,
                CriadoEm = novaAnotacao.CriadoEm
            };

            return CreatedAtAction(nameof(GetAnotacaoById), new { id = novaAnotacao.AnotacaoId }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnotacao(int id, [FromBody] UpdateAnotacaoDto dto)
        {
            var anotacao = await _uof.AnotacaoRepository.GetAnotacaoAsync(id);
            if (anotacao == null) return NotFound("Anotação não encontrada");

            anotacao.Titulo = dto.Titulo;
            anotacao.Conteudo = dto.Conteudo ?? string.Empty;
            anotacao.IsPublica = dto.IsPublica;
            anotacao.PastaId = dto.PastaId;

            await _uof.AnotacaoRepository.UpdateAnotacaoAsync(anotacao);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnotacao(int id)
        {
            await _uof.AnotacaoRepository.DeleteAnotacaoAsync(id);
            return NoContent();
        }
    }
}
