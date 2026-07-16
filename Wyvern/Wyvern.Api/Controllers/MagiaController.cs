using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Magia;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MagiaController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;

        public MagiaController(IUnitOfWork uof, IMapper mapper)
        {
            _uof = uof;
            _mapper = mapper;
        }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MagiaResponseDto>>> GetMagias()
    {
        var magias = await _uof.MagiaRepository.GetMagiasAsync();
        var magiasDto = _mapper.Map<List<MagiaResponseDto>>(magias);
        return Ok(magiasDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MagiaResponseDto>> GetMagiaById(int id)
    {
        var magia = await _uof.MagiaRepository.GetMagiaByIdAsync(id);
        if (magia == null) return NotFound("Magia não encontrada ou inativa.");
        var magiaDto = _mapper.Map<MagiaResponseDto>(magia);
        return Ok(magiaDto);
    }

    [HttpPost]
    public async Task<ActionResult<MagiaResponseDto>> CreateMagia(MagiaCreateDto magiaDto)
    {
        if (magiaDto == null)
        {
            return BadRequest("item inválido");
        }
        var magia = _mapper.Map<Magia>(magiaDto);
        await _uof.MagiaRepository.CreateMagiaAsync(magia);
        var magiaCriadaDto = _mapper.Map<MagiaResponseDto>(magia);
        return CreatedAtAction(nameof(GetMagiaById), new { id = magia.MagiaId }, magiaCriadaDto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateMagia(int id, MagiaUpdateDto magiaDto)
    {
        var magiaBanco = await _uof.MagiaRepository.GetMagiaByIdAsync(id);
        if (magiaBanco == null) return NotFound("Magia não encontrada.");
        _mapper.Map(magiaDto,magiaBanco);
        await _uof.MagiaRepository.UpdateMagiaAsync(magiaBanco);
        return Ok(_mapper.Map<MagiaResponseDto>(magiaBanco));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteMagia(int id)
    {
        var magia = await _uof.MagiaRepository.DeleteMagiaAsync(id);
        if (magia == null) return NotFound("Magia não encontrada.");

        magia.Ativo = false;
        return Ok(new { mensagem = "Magia desativada", id });
    }
}
}