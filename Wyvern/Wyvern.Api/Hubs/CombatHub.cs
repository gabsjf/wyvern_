using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Wyvern.Api.Hubs
{
    public class CombatHub : Hub
    {
        // Jogadores entram no grupo do combate específico
        public async Task JoinCombatGroup(string combateId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Combate_{combateId}");
        }

        public async Task LeaveCombatGroup(string combateId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Combate_{combateId}");
        }

        public async Task JoinSessaoGroup(string sessaoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Sessao_{sessaoId}");
        }

        public async Task LeaveSessaoGroup(string sessaoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Sessao_{sessaoId}");
        }
    }
}
