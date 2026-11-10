using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Application.DTOs.Sessao;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

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
        private readonly ICampanhaService _campanhaService;

        public CampanhaController(IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUserService,
            ICampanhaAuthorizationService campanhaAuth, ICampanhaService campanhaService)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _campanhaAuth = campanhaAuth;
            _campanhaService = campanhaService;
        }

        private int? UsuarioId => _currentUserService.UserId;

        private static string PapelDe(Campanha campanha, int? usuarioId) =>
            (campanha.Membros?.FirstOrDefault(m => m.UsuarioId == usuarioId)?.Papel ?? PapelCampanha.Jogador).ToString();

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CampanhaResponseDto>>> GetCampanha()
        {
            var campanhas = (await _uof.CampanhaRepository.GetCampanhasAsync()).ToList();
            var campanhasDto = _mapper.Map<List<CampanhaResponseDto>>(campanhas);

            // Papel do usuário em cada campanha (a lista já vem na mesma ordem)
            for (var i = 0; i < campanhasDto.Count; i++)
                campanhasDto[i].Papel = PapelDe(campanhas[i], UsuarioId);

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
            campanhaDto.Papel = PapelDe(campanha, UsuarioId);
            campanhaDto.Sessoes = _mapper.Map<List<SessaoResponseDto>>(campanha.Sessoes ?? new List<Sessao>());
            return Ok(campanhaDto);
        }

        // Conta Mestre com vaga no plano (ou qualquer conta durante o beta) cria a campanha e passa a ser o dono e Mestre
        // dela (CampanhaRepository.CreateCampanhaAsync registra o Mestre a partir do token, ignorando o DTO).
        [HttpPost]
        public async Task<ActionResult<CampanhaResponseDetailDto>> CreateCampanha(CreateCampanhaDto campanhaDto, [FromServices] IPlanoService planos)
        {
            if (campanhaDto == null)
                return BadRequest("Dados inválidos");

            if (UsuarioId is not { } usuarioId) return Unauthorized();
            await planos.GarantirPodeCriarCampanhaAsync(usuarioId); // sem vaga: 403 com o motivo

            var campanha = _mapper.Map<Campanha>(campanhaDto);

            await _uof.CampanhaRepository.CreateCampanhaAsync(campanha);
            await _uof.CommitAsync();

            var campanhaCompleta = await _uof.CampanhaRepository.GetCampanhaAsync(campanha.CampanhaId);

            if (campanhaCompleta == null)
                return CreatedAtAction(nameof(GetCampanhaById), new { id = campanha.CampanhaId }, null);

            var campanhaCriadaDto = _mapper.Map<CampanhaResponseDetailDto>(campanhaCompleta);
            campanhaCriadaDto.Papel = PapelDe(campanhaCompleta, UsuarioId);

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

            if (!await _campanhaAuth.IsMestreAsync(id, UsuarioId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode editar a campanha.");

            _mapper.Map(campanhaDto, campanhaNoBanco);

            await _uof.CampanhaRepository.UpdateCampanhaAsync(campanhaNoBanco);
            await _uof.CommitAsync();

            var campanhaAtualizada = await _uof.CampanhaRepository.GetCampanhaAsync(id);

            var campanhaDtoAtualizada = _mapper.Map<CampanhaResponseDetailDto>(campanhaAtualizada);
            campanhaDtoAtualizada.Papel = PapelDe(campanhaAtualizada!, UsuarioId);

            return Ok(campanhaDtoAtualizada);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteCampanha(int id)
        {
            var campanhaVisivel = await _uof.CampanhaRepository.GetCampanhaAsync(id);
            if (campanhaVisivel == null)
                return NotFound("Campanha não encontrada");

            if (!await _campanhaAuth.IsMestreAsync(id, UsuarioId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode excluir a campanha.");

            await _uof.CampanhaRepository.DeleteCampanhaAsync(id);
            await _uof.CommitAsync();
            return Ok("Campanha deletada com sucesso");
        }

        // O link é estável: chamar de novo devolve o mesmo token, então links já enviados continuam valendo
        // até expirarem. Só "renovar=true" troca o token (e invalida o link anterior).
        [HttpPost("{id:int}/generate-invite")]
        public async Task<ActionResult<ConviteDto>> GenerateInvite(int id, [FromQuery] bool renovar = false)
        {
            return Ok(await _campanhaService.GerarConviteAsync(id, UsuarioId, renovar));
        }

        // Desativa o link atual sem gerar outro.
        [HttpDelete("{id:int}/invite")]
        public async Task<ActionResult> RevokeInvite(int id)
        {
            await _campanhaService.RevogarConviteAsync(id, UsuarioId);
            return NoContent();
        }

        [HttpPost("join/{token}")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> Join(string token)
        {
            var campanhaId = await _campanhaService.EntrarPorConviteAsync(token, UsuarioId);
            return Ok(new { campanhaId });
        }

        [HttpGet("{id:int}/membros")]
        public async Task<ActionResult<IEnumerable<MembroDto>>> GetMembros(int id)
        {
            return Ok(await _campanhaService.ListarMembrosAsync(id, UsuarioId));
        }

        [HttpPut("{id:int}/membros/{usuarioId:int}/papel")]
        public async Task<ActionResult> AlterarPapel(int id, int usuarioId, AlterarPapelDto dto)
        {
            if (!Enum.TryParse<PapelCampanha>(dto.Papel, ignoreCase: true, out var papel) || !Enum.IsDefined(papel))
                return BadRequest("Papel inválido. Use \"Mestre\" ou \"Jogador\".");

            await _campanhaService.AlterarPapelAsync(id, usuarioId, papel, UsuarioId);
            return NoContent();
        }

        // O Mestre remove um membro; o próprio usuário usa esta rota para sair da campanha.
        [HttpDelete("{id:int}/membros/{usuarioId:int}")]
        public async Task<ActionResult> RemoverMembro(int id, int usuarioId)
        {
            await _campanhaService.RemoverMembroAsync(id, usuarioId, UsuarioId);
            return NoContent();
        }
    }
}
