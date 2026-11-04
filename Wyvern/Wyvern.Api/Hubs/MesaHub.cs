using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wyvern.Application.Services;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Api.Hubs
{
    /// <summary>A mesa ao vivo de uma campanha: quem participa vê, em tempo real, o que os outros rolam.</summary>
    [Authorize]
    public class MesaHub : Hub
    {
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly HistoricoRolagens _historico;

        public MesaHub(ICampanhaAuthorizationService campanhaAuth, HistoricoRolagens historico)
        {
            _campanhaAuth = campanhaAuth;
            _historico = historico;
        }

        private int? UsuarioId =>
            int.TryParse(Context.User?.FindFirst("nameid")?.Value, out var id) ? id : null;

        private string NomeUsuario => Context.User?.FindFirst("unique_name")?.Value ?? "Jogador";

        private static string Grupo(int campanhaId) => $"Mesa_{campanhaId}";

        private async Task ExigirMembroAsync(int campanhaId)
        {
            if (!await _campanhaAuth.IsMembroAsync(campanhaId, UsuarioId))
                throw new HubException("Campanha não encontrada.");
        }

        /// <summary>Entra na mesa da campanha e devolve as últimas rolagens.</summary>
        public async Task<IReadOnlyList<RolagemMesa>> EntrarNaMesa(int campanhaId)
        {
            await ExigirMembroAsync(campanhaId);
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(campanhaId));
            return _historico.Recentes(campanhaId);
        }

        public Task SairDaMesa(int campanhaId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(campanhaId));

        public async Task Rolar(int campanhaId, RolagemEntradaDto entrada)
        {
            await ExigirMembroAsync(campanhaId);

            RolagemMesa rolagem;
            try
            {
                rolagem = RolagemMesaFactory.Criar(UsuarioId!.Value, NomeUsuario, entrada);
            }
            catch (RegraNegocioException ex)
            {
                throw new HubException(ex.Message);
            }

            _historico.Adicionar(campanhaId, rolagem);
            await Clients.Group(Grupo(campanhaId)).SendAsync("ReceberRolagem", rolagem);
        }
    }
}
