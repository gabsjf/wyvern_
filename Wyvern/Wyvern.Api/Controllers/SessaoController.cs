using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Sessao;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers;

[Authorize]
[ApiController]
[Route("[Controller]")]
public class SessaoController : ControllerBase
{
    private readonly IUnitOfWork _uof;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ICampanhaAuthorizationService _campanhaAuth;
    private readonly ICronicaService _cronica;

    public SessaoController(IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUser, ICampanhaAuthorizationService campanhaAuth, ICronicaService cronica)
    {
        _cronica = cronica;
        _uof = uof;
        _mapper = mapper;
        _currentUser = currentUser;
        _campanhaAuth = campanhaAuth;
    }

    private ActionResult SomenteMestre(string acao) =>
        Problem(statusCode: StatusCodes.Status403Forbidden, detail: $"Apenas o Mestre da campanha pode {acao}.");

    // Só devolve sessões das campanhas de que o usuário participa (filtro no repositório).
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessaoResponseDto>>> GetSessao()
    {
        var sessions = await _uof.SessaoRepository.GetSessoesAsync();
        return Ok(_mapper.Map<List<SessaoResponseDto>>(sessions));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessaoResponseDto>> GetSessaoById(int id)
    {
        var session = await _uof.SessaoRepository.GetSessaoAsync(id);
        if (session == null)
        {
            return NotFound("Sessao não encontrado");
        }
        var sessionDto = _mapper.Map<SessaoResponseDto>(session);
        return Ok(sessionDto);
    }


    [HttpPost]
    public async Task<ActionResult<SessaoResponseDto>> CreateSessao(CreateSessaoDto sessaoDto)
    {
        if (sessaoDto == null) return BadRequest("Dados inválidos");

        if (!await _campanhaAuth.IsMestreAsync(sessaoDto.CampanhaId, _currentUser.UserId))
            return SomenteMestre("criar sessões");

        var sessao = _mapper.Map<Sessao>(sessaoDto);
        await _uof.SessaoRepository.CreateSessaoAsync(sessao);
        await _uof.CommitAsync();

        // A sessão abre um capítulo na crônica da campanha
        await _cronica.RegistrarAutomaticoAsync(sessao.CampanhaId, "Marco", $"Sessão {sessao.NumeroSessao}: {sessao.Nome}", string.Empty, sessao.SessaoId);
        await _uof.CommitAsync();

        var sessaoCompleta = await _uof.SessaoRepository.GetSessaoAsync(sessao.SessaoId);
        if (sessaoCompleta == null)
        {
            return CreatedAtAction(nameof(GetSessaoById), new { id = sessao.SessaoId }, null);
        }
        var sessaoCriadaDto = _mapper.Map<SessaoResponseDto>(sessaoCompleta);
        return CreatedAtAction(nameof(GetSessaoById), new { id = sessao.SessaoId }, sessaoCriadaDto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateSessao(int id, UpdateSessaoDto sessaoDto)
    {
        var sessaoBanco = await _uof.SessaoRepository.GetSessaoAsync(id);
        if (sessaoBanco == null) return NotFound("Sessão não encontrada.");

        if (!await _campanhaAuth.IsMestreAsync(sessaoBanco.CampanhaId, _currentUser.UserId))
            return SomenteMestre("editar sessões");

        // Mover a sessão para outra campanha exige ser Mestre das duas.
        if (sessaoBanco.CampanhaId != sessaoDto.CampanhaId)
        {
            var campanhaDestino = await _uof.CampanhaRepository.GetCampanhaAsync(sessaoDto.CampanhaId);
            if (campanhaDestino == null) return BadRequest("A nova campanha informada não existe");

            if (!await _campanhaAuth.IsMestreAsync(sessaoDto.CampanhaId, _currentUser.UserId))
                return SomenteMestre("mover sessões para esta campanha");
        }

        _mapper.Map(sessaoDto, sessaoBanco);

        await _uof.SessaoRepository.UpdateSessaoAsync(sessaoBanco);
        await _uof.CommitAsync();
        var sessaoAtualizadaDto = _mapper.Map<SessaoResponseDto>(sessaoBanco);
        return Ok(sessaoAtualizadaDto);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteSessao(int id)
    {
        var sessao = await _uof.SessaoRepository.GetSessaoAsync(id);
        if (sessao == null) return NotFound("Sessão não encontrada");

        if (!await _campanhaAuth.IsMestreAsync(sessao.CampanhaId, _currentUser.UserId))
            return SomenteMestre("excluir sessões");

        await _uof.SessaoRepository.DeleteSessaoAsync(id);
        await _uof.CommitAsync();
        return Ok(new { mensagem = "Sessão deletada com sucesso" });
    }

}
