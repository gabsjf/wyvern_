using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Pericia;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PericiaController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;

        public PericiaController(IUnitOfWork uof, IMapper mapper)
        {
            _uof = uof;
            _mapper = mapper;
        }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PericiaResponseDto>>> GetPericias()
    {
        var pericias = await _uof.PericiaRepository.GetPericiasAsync();
        if (!pericias.Any()) {
            return NotFound("Pericia não encontrada");
        }
        var periciasDto = _mapper.Map<List<PericiaResponseDto>>(pericias);
        return Ok(periciasDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PericiaResponseDto>> GetPericiaById(int id)
    {
        var pericia = await _uof.PericiaRepository.GetPericiaAsync(id);
        if (pericia == null) return NotFound("Perícia não encontrada.");
        var periciaDto = _mapper.Map<PericiaResponseDto>(pericia);
        return Ok(periciaDto);
    }

    [HttpPost]
    public async Task<ActionResult<PericiaResponseDto>> CreatePericia(CreatePericiaDto periciaDto)
    {
        if(periciaDto == null)
        {
            return BadRequest("pericia inválida");
        }
        var pericia = _mapper.Map<Pericia>(periciaDto);
        await _uof.PericiaRepository.CreatePericiaAsync(pericia);
        var periciaCriadaDto = _mapper.Map<PericiaResponseDto>(pericia);
        return CreatedAtAction(nameof(GetPericiaById), new { id = pericia.PericiaId }, periciaCriadaDto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdatePericia(int id, PericiaUpdateDto periciaDto)
    {
        var periciaBanco = await _uof.PericiaRepository.GetPericiaAsync(id);
        if (periciaBanco == null) return NotFound("Perícia não encontrada.");
        _mapper.Map(periciaDto, periciaBanco);
        await _uof.PericiaRepository.UpdatePericiaAsync(periciaBanco);
        return Ok(_mapper.Map<PericiaResponseDto>(periciaBanco));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePericia(int id)
    {
        var pericia = await _uof.PericiaRepository.DeletePericiaAsync(id);
        if (pericia == null) return NotFound("Perícia não encontrada.");
        return Ok(new { mensagem = "Perícia desativada", id });
    }
}
}