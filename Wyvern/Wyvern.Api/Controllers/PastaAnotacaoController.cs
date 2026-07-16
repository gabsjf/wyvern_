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
    public class PastaAnotacaoController : ControllerBase
    {
        private readonly IUnitOfWork _uof;

        public PastaAnotacaoController(IUnitOfWork uof)
        {
            _uof = uof;
        }

        [HttpGet("campanha/{campanhaId}")]
        public async Task<ActionResult<IEnumerable<PastaAnotacao>>> GetPastas(int campanhaId)
        {
            var pastas = await _uof.PastaAnotacaoRepository.GetPastasByCampanhaAsync(campanhaId);
            return Ok(pastas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PastaAnotacao>> GetPastaById(int id)
        {
            var p = await _uof.PastaAnotacaoRepository.GetPastaAsync(id);
            if (p == null) return NotFound("Pasta não encontrada");
            return Ok(p);
        }

        [HttpPost]
        public async Task<ActionResult<PastaAnotacao>> CreatePasta([FromBody] PastaAnotacao dto)
        {
            var novaPasta = new PastaAnotacao
            {
                CampanhaId = dto.CampanhaId,
                Nome = dto.Nome,
                IsPublica = dto.IsPublica,
                CriadoEm = System.DateTime.Now
            };

            await _uof.PastaAnotacaoRepository.CreatePastaAsync(novaPasta);
            return CreatedAtAction(nameof(GetPastaById), new { id = novaPasta.PastaId }, novaPasta);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePasta(int id, [FromBody] PastaAnotacao dto)
        {
            var pasta = await _uof.PastaAnotacaoRepository.GetPastaAsync(id);
            if (pasta == null) return NotFound("Pasta não encontrada");

            pasta.Nome = dto.Nome;
            pasta.IsPublica = dto.IsPublica;

            await _uof.PastaAnotacaoRepository.UpdatePastaAsync(pasta);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasta(int id)
        {
            await _uof.PastaAnotacaoRepository.DeletePastaAsync(id);
            return NoContent();
        }
    }
}
