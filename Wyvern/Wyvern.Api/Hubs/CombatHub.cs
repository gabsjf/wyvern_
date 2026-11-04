using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Wyvern.Application.Services;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Hubs
{
    [Authorize]
    public class CombatHub : Hub
    {
        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;

        public CombatHub(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
        }

        // O id do usuário vem do token da conexão (não do HttpContextAccessor, que não é confiável em hubs).
        private int? UsuarioId =>
            int.TryParse(Context.User?.FindFirst("nameid")?.Value, out var id) ? id : null;

        // Jogadores entram no grupo do combate específico, mas só se participarem da campanha dele.
        public async Task JoinCombatGroup(string combateId)
        {
            if (!int.TryParse(combateId, out var id)) throw new HubException("Combate inválido.");

            var combate = await _uof.CombateRepository.GetCombateAsync(id);
            if (combate?.Sessao == null || !await _campanhaAuth.IsMembroAsync(combate.Sessao.CampanhaId, UsuarioId))
                throw new HubException("Combate não encontrado.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Combate_{combateId}");
        }

        public async Task LeaveCombatGroup(string combateId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Combate_{combateId}");
        }

        public async Task JoinSessaoGroup(string sessaoId)
        {
            if (!int.TryParse(sessaoId, out var id)) throw new HubException("Sessão inválida.");

            var campanhaId = await _uof.SessaoRepository.GetCampanhaIdAsync(id);
            if (!campanhaId.HasValue || !await _campanhaAuth.IsMembroAsync(campanhaId.Value, UsuarioId))
                throw new HubException("Sessão não encontrada.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Sessao_{sessaoId}");
        }

        public async Task LeaveSessaoGroup(string sessaoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Sessao_{sessaoId}");
        }
    }
}
