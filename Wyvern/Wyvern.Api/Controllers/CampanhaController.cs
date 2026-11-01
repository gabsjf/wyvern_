using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Application.DTOs.Sessao;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Wyvern.Domain.Interfaces;
using Wyvern.Application.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[Controller]")]
    public class CampanhaController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICampanhaAuthorizationService _campanhaAuth;

        public CampanhaController(IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUserService, ICampanhaAuthorizationService campanhaAuth)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _campanhaAuth = campanhaAuth;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CampanhaResponseDto>>> GetCampanha()
        {
            var campanhas = await _uof.CampanhaRepository.GetCampanhasAsync();
            var currentUserId = _currentUserService.UserId;
            var campanhasDto = _mapper.Map<List<CampanhaResponseDto>>(campanhas);
            
            // Set Papel
            foreach (var dto in campanhasDto)
            {
                var camp = campanhas.First(c => c.CampanhaId == dto.CampanhaId);
                dto.Papel = _campanhaAuth.IsMestre(camp, currentUserId) ? "Mestre" : "Jogador";
            }
            
            return Ok(campanhasDto);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CampanhaResponseDetailDto>> GetCampanhaById (int id)
        {
            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(id);
            if ( campanha == null)
            {
                return NotFound("Campanha nao encontrada");
            }
            var campanhaDto = _mapper.Map<CampanhaResponseDetailDto>(campanha);
            campanhaDto.MestreNome = campanha.Mestre?.Nome ?? string.Empty;
            campanhaDto.Sessoes = _mapper.Map<List<SessaoResponseDto>>(campanha.Sessoes ?? new List<Sessao>());
            return Ok(campanhaDto);
        }
        // Qualquer usuário autenticado pode criar uma campanha e passa a ser o Mestre dela
        // (CampanhaRepository.CreateCampanhaAsync define MestreId a partir do token, ignorando o DTO).
        [HttpPost]
        public async Task<ActionResult<CampanhaResponseDetailDto>> CreateCampanha(CreateCampanhaDto campanhaDto)
        {
            if (campanhaDto == null)
                return BadRequest("Dados inválidos");

            var campanha = _mapper.Map<Campanha>(campanhaDto);

            await _uof.CampanhaRepository.CreateCampanhaAsync(campanha);
            await _uof.CommitAsync();

            var campanhaCompleta = await _uof.CampanhaRepository.GetCampanhaAsync(campanha.CampanhaId);

            if (campanhaCompleta == null)
                return CreatedAtAction(nameof(GetCampanhaById), new { id = campanha.CampanhaId }, null);

            var campanhaCriadaDto = _mapper.Map<CampanhaResponseDetailDto>(campanhaCompleta);

            return CreatedAtAction(nameof(GetCampanhaById), new { id = campanha.CampanhaId }, campanhaCriadaDto);
        }
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CampanhaResponseDetailDto>> UpdateCampanha(int id, CampanhaUpdatetDto campanhaDto)
        {
            if (campanhaDto == null)
                return BadRequest("Dados inválidos");

            var campanhaNoBanco = await _uof.CampanhaRepository.GetCampanhaAsync(id);

            if (campanhaNoBanco == null)
                return NotFound("Campanha não encontrada");

            if (!_campanhaAuth.IsMestre(campanhaNoBanco, _currentUserService.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode editar a campanha.");

            _mapper.Map(campanhaDto, campanhaNoBanco);

            await _uof.CampanhaRepository.UpdateCampanhaAsync(campanhaNoBanco);
            await _uof.CommitAsync();

            var campanhaAtualizada = await _uof.CampanhaRepository.GetCampanhaAsync(id);

            var campanhaDtoAtualizada = _mapper.Map<CampanhaResponseDetailDto>(campanhaAtualizada);

            return Ok(campanhaDtoAtualizada);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteCampanha(int id)
        {
            var campanhaVisivel = await _uof.CampanhaRepository.GetCampanhaAsync(id);
            if (campanhaVisivel == null)
                return NotFound("Campanha não encontrada");

            if (!_campanhaAuth.IsMestre(campanhaVisivel, _currentUserService.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode excluir a campanha.");

            await _uof.CampanhaRepository.DeleteCampanhaAsync(id);
            await _uof.CommitAsync();
            return Ok("Campanha deletada com sucesso");
        }

        // O link é estável: chamar de novo devolve o mesmo token, então links já enviados continuam valendo.
        // Só "renovar=true" troca o token (e invalida o link anterior).
        [HttpPost("{id:int}/generate-invite")]
        public async Task<ActionResult> GenerateInvite(int id, [FromQuery] bool renovar = false)
        {
            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(id);
            if (campanha == null) return NotFound("Campanha não encontrada");

            var currentUserId = _currentUserService.UserId;
            if (!_campanhaAuth.IsMestre(campanha, currentUserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode gerar convites.");

            if (renovar || string.IsNullOrEmpty(campanha.TokenConvite))
            {
                campanha.TokenConvite = Guid.NewGuid().ToString("N");
                await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
                await _uof.CommitAsync();
            }

            return Ok(new { token = campanha.TokenConvite });
        }

        // Desativa o link atual sem gerar outro.
        [HttpDelete("{id:int}/invite")]
        public async Task<ActionResult> RevokeInvite(int id)
        {
            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(id);
            if (campanha == null) return NotFound("Campanha não encontrada");

            if (!_campanhaAuth.IsMestre(campanha, _currentUserService.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode revogar convites.");

            campanha.TokenConvite = null;
            await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
            await _uof.CommitAsync();
            return NoContent();
        }

        [HttpPost("join/{token}")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> Join(string token)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value <= 0) return Unauthorized();

            // Use custom repository method that does not filter by user
            var campanha = await _uof.CampanhaRepository.GetCampanhaByTokenAsync(token);
            
            if (campanha == null) return NotFound("Convite inválido ou expirado.");
            if (_campanhaAuth.IsMestre(campanha, currentUserId)) return BadRequest("Você já é o mestre desta campanha.");

            // Since we don't have a direct CampanhaJogadorRepository in IUnitOfWork yet, we can use the DbContext directly for MVP
            // Let's assume we can inject DbContext or use a raw SQL / generic repo if available.
            // But wait, the repo might not have it. Let's add it via Campanha.Jogadores.
            
            if (campanha.Jogadores == null) campanha.Jogadores = new List<CampanhaJogador>();
            if (!campanha.Jogadores.Any(j => j.UsuarioId == currentUserId.Value))
            {
                campanha.Jogadores.Add(new CampanhaJogador { UsuarioId = currentUserId.Value, CampanhaId = campanha.CampanhaId });
                await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
                await _uof.CommitAsync();
            }
            
            return Ok(new { campanhaId = campanha.CampanhaId });
        }
    }
}
