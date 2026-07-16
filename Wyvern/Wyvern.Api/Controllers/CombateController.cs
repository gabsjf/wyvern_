using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Combate;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CombateController : ControllerBase
    {
        private readonly IUnitOfWork _uof;

        public CombateController(IUnitOfWork uof)
        {
            _uof = uof;
        }

        [HttpGet("{id}/participantes")]
        public async Task<ActionResult<IEnumerable<CombateParticipanteDto>>> GetParticipantes(int id)
        {
            var combate = await _uof.CombateRepository.GetCombateAsync(id);
            if (combate == null || combate.Participantes == null) return Ok(new List<CombateParticipanteDto>());

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
            int sessaoId = dto.SessaoId ?? 1;

            // Se SessaoId nao foi providenciado mas CampanhaId sim, buscamos a última sessão ou criamos
            if (!dto.SessaoId.HasValue && dto.CampanhaId.HasValue)
            {
                var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(dto.CampanhaId.Value);
                if (campanha != null && campanha.Sessoes != null && campanha.Sessoes.Any())
                {
                    sessaoId = campanha.Sessoes.Last().SessaoId;
                }
                else if (campanha != null)
                {
                    // Criar uma sessao fake para comportar o combate
                    var novaSessao = new Sessao { CampanhaId = campanha.CampanhaId, Nome = "Sessão Automática", NumeroSessao = 1 };
                    await _uof.SessaoRepository.CreateSessaoAsync(novaSessao);
                    sessaoId = novaSessao.SessaoId;
                }
            }

            var combate = new Wyvern.Domain.Entities.Combate
            {
                SessaoId = sessaoId,
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
            return Ok(combate);
        }

        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndCombate(int id)
        {
            await _uof.CombateRepository.DeleteCombateAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/next-turn")]
        public async Task<IActionResult> NextTurn(int id)
        {
            var combate = await _uof.CombateRepository.GetCombateAsync(id);
            if (combate != null && combate.Participantes != null && combate.Participantes.Any())
            {
                var participantesSorted = combate.Participantes.OrderByDescending(p => p.Iniciativa).ToList();
                int originalIndex = combate.TurnoAtualIndex;
                do
                {
                    combate.TurnoAtualIndex++;
                    if (combate.TurnoAtualIndex >= participantesSorted.Count)
                    {
                        combate.TurnoAtualIndex = 0;
                        combate.RodadaAtual++;
                    }

                    var p = participantesSorted[combate.TurnoAtualIndex];
                    bool isMorto = (p.IsInimigo && p.VidaAtual <= 0) || (!p.IsInimigo && p.FalhasMorte >= 3);
                    if (!isMorto) break;

                } while (combate.TurnoAtualIndex != originalIndex);

                await _uof.CombateRepository.UpdateCombateAsync(combate);
            }
            return NoContent();
        }

        [HttpPut("{id}/participantes/{participanteId}")]
        public async Task<IActionResult> UpdateParticipante(int id, int participanteId, [FromBody] CombateParticipanteDto dto)
        {
            var combate = await _uof.CombateRepository.GetCombateAsync(id);
            if (combate == null) return NotFound("Combate não encontrado");

            var participante = combate.Participantes?.FirstOrDefault(p => p.ParticipanteId == participanteId);
            if (participante == null) return NotFound("Participante não encontrado");

            participante.VidaAtual = dto.VidaAtual;
            participante.Condicoes = dto.Condicoes ?? string.Empty;
            participante.SucessosMorte = dto.SucessosMorte;
            participante.FalhasMorte = dto.FalhasMorte;

            await _uof.CombateRepository.UpdateParticipanteAsync(participante);
            return NoContent();
        }
    }
}
