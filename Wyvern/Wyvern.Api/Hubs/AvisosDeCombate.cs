using Microsoft.AspNetCore.SignalR;
using Wyvern.Domain.Entities;

namespace Wyvern.Api.Hubs
{
    /// <summary>Avisa as mesas quando a ficha de um herói e o combate dele mudaram juntos.</summary>
    public static class AvisosDeCombate
    {
        /// <summary>
        /// O rastreador e o mapa de cada combate recarregam ("ReceiveCombatUpdate"); na sessão, quem mostra a ficha do herói
        /// (a ficha rápida do jogador) recebe "PersonagemAtualizado" com o id dele.
        /// </summary>
        public static async Task AvisarAsync(this IHubContext<CombatHub> hub, IEnumerable<Combate> combates, int personagemId)
        {
            foreach (var combate in combates.DistinctBy(c => c.CombateId))
            {
                await hub.Clients.Group($"Combate_{combate.CombateId}").SendAsync("ReceiveCombatUpdate");
                await hub.Clients.Group($"Sessao_{combate.SessaoId}").SendAsync("PersonagemAtualizado", personagemId);
            }
        }
    }
}
