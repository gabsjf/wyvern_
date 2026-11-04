using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Cronica;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    /// <summary>
    /// A crônica da campanha. Todos os membros leem e escrevem; o Mestre vê tudo e pode esconder eventos (spoilers);
    /// cada jogador edita só o que escreveu. Quem não participa da campanha não sabe que ela existe (404).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Eventos")]
    public class CronicaController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;

        public CronicaController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
        }

        private async Task<PapelCampanha?> PapelAsync(int campanhaId) =>
            await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventoCampanhaDto>>> Listar(int campanhaId)
        {
            var papel = await PapelAsync(campanhaId);
            if (papel == null) return NotFound("Campanha não encontrada.");

            var eventos = await _uof.EventoCampanhaRepository.GetPorCampanhaAsync(campanhaId);
            return Ok(eventos.Where(e => papel == PapelCampanha.Mestre || !e.SoMestre).Select(EventoCampanhaDto.De));
        }

        [HttpPost]
        public async Task<ActionResult<EventoCampanhaDto>> Criar(int campanhaId, EventoCampanhaFormDto form)
        {
            var papel = await PapelAsync(campanhaId);
            if (papel == null) return NotFound("Campanha não encontrada.");

            var erro = await ValidarAsync(campanhaId, form);
            if (erro != null) return erro;

            var evento = new EventoCampanha
            {
                CampanhaId = campanhaId,
                CriadoPorId = _currentUser.UserId,
                CriadoEm = DateTime.UtcNow,
                Ordem = form.Ordem ?? await _uof.EventoCampanhaRepository.MaiorOrdemAsync(campanhaId) + 1,
            };
            Preencher(evento, form, papel == PapelCampanha.Mestre);

            await _uof.EventoCampanhaRepository.AddAsync(evento);
            await _uof.CommitAsync();

            var salvo = await _uof.EventoCampanhaRepository.GetAsync(campanhaId, evento.EventoId);
            return Ok(EventoCampanhaDto.De(salvo!));
        }

        [HttpPut("{eventoId:int}")]
        public async Task<ActionResult<EventoCampanhaDto>> Editar(int campanhaId, int eventoId, EventoCampanhaFormDto form)
        {
            var (evento, papel, erroAcesso) = await CarregarAsync(campanhaId, eventoId);
            if (erroAcesso != null) return erroAcesso;

            var erro = await ValidarAsync(campanhaId, form);
            if (erro != null) return erro;

            // Eventos que o sistema registrou só o Mestre mexe; os demais, o autor ou o Mestre.
            Preencher(evento!, form, papel == PapelCampanha.Mestre);
            if (form.Ordem.HasValue) evento!.Ordem = form.Ordem.Value;

            await _uof.CommitAsync();
            var salvo = await _uof.EventoCampanhaRepository.GetAsync(campanhaId, eventoId);
            return Ok(EventoCampanhaDto.De(salvo!));
        }

        [HttpDelete("{eventoId:int}")]
        public async Task<IActionResult> Excluir(int campanhaId, int eventoId)
        {
            var (evento, _, erroAcesso) = await CarregarAsync(campanhaId, eventoId);
            if (erroAcesso != null) return erroAcesso;

            await _uof.EventoCampanhaRepository.RemoveAsync(evento!);
            await _uof.CommitAsync();
            return NoContent();
        }

        /// <summary>Carrega o evento e confere se quem pede pode mexer nele.</summary>
        private async Task<(EventoCampanha? evento, PapelCampanha? papel, ActionResult? erro)> CarregarAsync(int campanhaId, int eventoId)
        {
            var papel = await PapelAsync(campanhaId);
            if (papel == null) return (null, null, NotFound("Campanha não encontrada."));

            var evento = await _uof.EventoCampanhaRepository.GetAsync(campanhaId, eventoId);
            // Para quem não é Mestre, um evento escondido simplesmente não existe.
            if (evento == null || (evento.SoMestre && papel != PapelCampanha.Mestre))
                return (null, papel, NotFound("Evento não encontrado."));

            var podeMexer = papel == PapelCampanha.Mestre || (!evento.Automatico && evento.CriadoPorId == _currentUser.UserId);
            if (!podeMexer)
                return (null, papel, Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Você só pode alterar os eventos que escreveu."));

            return (evento, papel, null);
        }

        private async Task<ActionResult?> ValidarAsync(int campanhaId, EventoCampanhaFormDto form)
        {
            if (string.IsNullOrWhiteSpace(form.Titulo))
                return BadRequest(new { message = "Dê um título ao evento." });
            if (!TiposDeEvento.Valido(form.Tipo))
                return BadRequest(new { message = $"Tipo inválido. Use: {string.Join(", ", TiposDeEvento.Todos)}." });

            if (form.SessaoId is { } sessaoId)
            {
                var da = await _uof.SessaoRepository.GetCampanhaIdAsync(sessaoId);
                if (da != campanhaId) return BadRequest(new { message = "A sessão informada não é desta campanha." });
            }
            return null;
        }

        private static void Preencher(EventoCampanha evento, EventoCampanhaFormDto form, bool ehMestre)
        {
            evento.Titulo = form.Titulo.Trim();
            evento.Descricao = form.Descricao ?? string.Empty;
            evento.Tipo = form.Tipo;
            evento.SessaoId = form.SessaoId;
            evento.RotuloData = string.IsNullOrWhiteSpace(form.RotuloData) ? null : form.RotuloData.Trim();
            // Só o Mestre esconde eventos; para os jogadores o campo é ignorado (e não "desesconde" o que o Mestre escondeu)
            if (ehMestre) evento.SoMestre = form.SoMestre;
        }
    }
}
