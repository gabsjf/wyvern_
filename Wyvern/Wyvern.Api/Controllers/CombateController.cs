using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Combate;
using Wyvern.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wyvern.Api.Hubs;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Wyvern.Application.Services;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CombateController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IHubContext<CombatHub> _hubContext;
        private readonly ICombateService _combateService;
        private readonly ICurrentUserService _currentUser;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICronicaService _cronica;

        public CombateController(IUnitOfWork uof, IHubContext<CombatHub> hubContext, ICombateService combateService,
            ICurrentUserService currentUser, ICampanhaAuthorizationService campanhaAuth, ICronicaService cronica)
        {
            _cronica = cronica;
            _uof = uof;
            _hubContext = hubContext;
            _combateService = combateService;
            _currentUser = currentUser;
            _campanhaAuth = campanhaAuth;
        }

        private ActionResult Proibido(string detalhe) =>
            Problem(statusCode: StatusCodes.Status403Forbidden, detail: detalhe);

        // Combate inexistente e combate de outra campanha respondem igual (404), para não revelar ids.
        private async Task<(Combate? combate, ActionResult? erro)> CarregarCombateAsync(int id, bool exigeMestre)
        {
            var combate = await _uof.CombateRepository.GetCombateAsync(id);
            if (combate?.Sessao == null) return (null, NotFound("Combate não encontrado"));

            var campanhaId = combate.Sessao.CampanhaId;
            var autorizado = exigeMestre
                ? await _campanhaAuth.IsMestreAsync(campanhaId, _currentUser.UserId)
                : await _campanhaAuth.IsMembroAsync(campanhaId, _currentUser.UserId);

            if (!autorizado)
                return (null, exigeMestre ? Proibido("Apenas o Mestre pode controlar o combate.") : NotFound("Combate não encontrado"));

            return (combate, null);
        }

        [HttpGet("{id}/participantes")]
        public async Task<ActionResult<IEnumerable<CombateParticipanteDto>>> GetParticipantes(int id)
        {
            var (combate, erro) = await CarregarCombateAsync(id, exigeMestre: false);
            if (erro != null) return erro;
            if (combate!.Participantes == null) return Ok(new List<CombateParticipanteDto>());

            var result = combate.Participantes.OrderByDescending(p => p.Iniciativa).Select(p => new CombateParticipanteDto
            {
                ParticipanteId = p.ParticipanteId,
                CombateId = p.CombateId,
                PersonagemId = p.PersonagemId,
                NomeNPC = p.NomeNPC,
                Iniciativa = p.Iniciativa,
                VidaAtual = p.VidaAtual,
                VidaMaxima = p.VidaMaxima,
                ClasseArmadura = p.ClasseArmadura,
                IsInimigo = p.IsInimigo,
                Condicoes = p.Condicoes,
                SucessosMorte = p.SucessosMorte,
                FalhasMorte = p.FalhasMorte
            });

            return Ok(result);
        }

        [HttpGet("active/{campanhaId}")]
        public async Task<ActionResult<Wyvern.Domain.Entities.Combate>> GetActiveCombate(int campanhaId)
        {
            if (!await _campanhaAuth.IsMembroAsync(campanhaId, _currentUser.UserId))
                return Proibido("Você não participa desta campanha.");

            var combate = await _uof.CombateRepository.GetActiveCombateByCampanhaAsync(campanhaId);
            if (combate == null) return Ok(null);

            // Order participants by iniciativa descending when fetching
            if (combate.Participantes != null)
            {
                combate.Participantes = combate.Participantes.OrderByDescending(p => p.Iniciativa).ToList();
            }

            return Ok(combate);
        }

        [HttpGet("active/sessao/{sessaoId}")]
        public async Task<ActionResult<Wyvern.Domain.Entities.Combate>> GetActiveCombateBySessao(int sessaoId)
        {
            // GetSessaoAsync só devolve sessões de campanhas de que o usuário participa.
            var sessao = await _uof.SessaoRepository.GetSessaoAsync(sessaoId);
            if (sessao == null) return Proibido("Você não participa desta campanha.");

            var combate = await _uof.CombateRepository.GetActiveCombateBySessaoAsync(sessaoId);
            if (combate == null) return Ok(null);

            if (combate.Participantes != null)
            {
                combate.Participantes = combate.Participantes.OrderByDescending(p => p.Iniciativa).ToList();
            }

            return Ok(combate);
        }

        [HttpPost("start")]
        public async Task<ActionResult<CombateCreateDto>> StartCombate([FromBody] CombateCreateDto dto)
        {
            if (!dto.SessaoId.HasValue && !dto.CampanhaId.HasValue)
                return BadRequest("Informe a sessão ou a campanha do combate.");

            int campanhaId;
            int sessaoId = 0;
            Sessao? novaSessaoPendente = null;

            if (dto.SessaoId.HasValue)
            {
                var sessao = await _uof.SessaoRepository.GetSessaoAsync(dto.SessaoId.Value);
                if (sessao == null) return NotFound("Sessão não encontrada");

                campanhaId = sessao.CampanhaId;
                sessaoId = sessao.SessaoId;
            }
            else
            {
                campanhaId = dto.CampanhaId!.Value;
            }

            if (!await _campanhaAuth.IsMestreAsync(campanhaId, _currentUser.UserId))
                return Proibido("Apenas o Mestre pode iniciar um combate.");

            // Só personagens ativos desta campanha que o Mestre enxerga podem entrar no combate.
            var idsPersonagens = dto.Participantes?.Where(p => p.PersonagemId.HasValue).Select(p => p.PersonagemId!.Value).Distinct().ToList()
                                 ?? new List<int>();
            if (idsPersonagens.Count > 0)
            {
                var permitidos = (await _uof.PersonagemRepository.GetPersonagensAsync())
                    .Where(p => p.CampanhaId == campanhaId)
                    .Select(p => p.PersonagemId)
                    .ToHashSet();

                if (idsPersonagens.Any(pid => !permitidos.Contains(pid)))
                    return BadRequest("Há participantes que não pertencem a esta campanha.");
            }

            // Se a sessão não foi informada, usa a última da campanha ou cria uma.
            if (!dto.SessaoId.HasValue)
            {
                var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(campanhaId);
                if (campanha == null) return NotFound("Campanha não encontrada");

                if (campanha.Sessoes != null && campanha.Sessoes.Any())
                {
                    sessaoId = campanha.Sessoes.Last().SessaoId;
                }
                else
                {
                    // O Id real só existe após o Commit, então a ligação com o Combate é feita pela navegação (Sessao).
                    novaSessaoPendente = new Sessao { CampanhaId = campanha.CampanhaId, Nome = "Sessão Automática", NumeroSessao = 1 };
                    await _uof.SessaoRepository.CreateSessaoAsync(novaSessaoPendente);
                }
            }

            // Uma sessão só pode ter um combate ativo (o banco também garante, por índice único).
            if (sessaoId != 0 && await _uof.CombateRepository.GetActiveCombateBySessaoAsync(sessaoId) != null)
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "Já existe um combate ativo nesta sessão. Encerre-o antes de iniciar outro.");

            var combate = new Wyvern.Domain.Entities.Combate
            {
                SessaoId = sessaoId,
                Sessao = novaSessaoPendente,
                RodadaAtual = 1,
                TurnoAtualIndex = 0,
                Ativo = true,
                CriadoEm = DateTime.Now,
                Participantes = dto.Participantes?.Select(p => new CombateParticipante
                {
                    PersonagemId = p.PersonagemId,
                    NomeNPC = p.NomeNPC,
                    Iniciativa = p.Iniciativa,
                    VidaAtual = p.VidaAtual,
                    VidaMaxima = p.VidaMaxima,
                    ClasseArmadura = p.ClasseArmadura,
                    IsInimigo = p.IsInimigo,
                    Condicoes = p.Condicoes
                }).ToList()
            };

            await _uof.CombateRepository.CreateCombateAsync(combate);
            await _uof.CommitAsync();

            if (novaSessaoPendente != null)
            {
                sessaoId = novaSessaoPendente.SessaoId;
            }

            await _hubContext.Clients.Group($"Combate_{combate.CombateId}").SendAsync("ReceiveCombatUpdate");
            await _hubContext.Clients.Group($"Sessao_{sessaoId}").SendAsync("ReceiveCombatUpdate");
            return Ok(combate);
        }

        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndCombate(int id)
        {
            var (combate, erro) = await CarregarCombateAsync(id, exigeMestre: true);
            if (erro != null) return erro;

            // O combate some ao terminar; antes disso, o que aconteceu nele vai para a crônica da campanha.
            var (titulo, descricao) = ResumoDeCombate.Criar(combate!);
            await _cronica.RegistrarAutomaticoAsync(combate!.Sessao!.CampanhaId, "Combate", titulo, descricao, combate.SessaoId);

            await _uof.CombateRepository.DeleteCombateAsync(id);
            await _uof.CommitAsync();
            await _hubContext.Clients.Group($"Combate_{id}").SendAsync("ReceiveCombatUpdate");
            await _hubContext.Clients.Group($"Sessao_{combate!.SessaoId}").SendAsync("ReceiveCombatUpdate");
            return NoContent();
        }

        [HttpPost("{id}/next-turn")]
        public async Task<IActionResult> NextTurn(int id)
        {
            var (_, erro) = await CarregarCombateAsync(id, exigeMestre: true);
            if (erro != null) return erro;

            var combate = await _combateService.AvancarTurnoAsync(id);
            if (combate != null && combate.Participantes != null && combate.Participantes.Any())
            {
                await _hubContext.Clients.Group($"Combate_{id}").SendAsync("ReceiveCombatUpdate");
            }
            return NoContent();
        }

        [HttpPut("{id}/participantes/{participanteId}")]
        public async Task<IActionResult> UpdateParticipante(int id, int participanteId, [FromBody] CombateParticipanteDto dto)
        {
            var (combate, erro) = await CarregarCombateAsync(id, exigeMestre: false);
            if (erro != null) return erro;

            var participante = combate!.Participantes?.FirstOrDefault(p => p.ParticipanteId == participanteId);
            if (participante == null) return NotFound("Participante não encontrado");

            // O Mestre altera qualquer participante; jogadores, só o próprio personagem.
            var isMestre = await _campanhaAuth.IsMestreAsync(combate.Sessao!.CampanhaId, _currentUser.UserId);
            var ehProprioPersonagem = participante.Personagem != null && participante.Personagem.CriadoPorId == _currentUser.UserId;
            if (!isMestre && !ehProprioPersonagem)
                return Proibido("Você só pode alterar o seu próprio personagem.");

            participante.VidaAtual = dto.VidaAtual;
            participante.Condicoes = dto.Condicoes ?? string.Empty;
            participante.SucessosMorte = dto.SucessosMorte;
            participante.FalhasMorte = dto.FalhasMorte;

            await _uof.CombateRepository.UpdateParticipanteAsync(participante);
            await _uof.CommitAsync();
            await _hubContext.Clients.Group($"Combate_{id}").SendAsync("ReceiveCombatUpdate");
            return NoContent();
        }
    }
}
