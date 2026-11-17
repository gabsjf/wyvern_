using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Api.Hubs
{
    /// <summary>
    /// A mesa ao vivo de uma campanha: quem participa vê, em tempo real, o que os outros rolam.
    /// As rolagens do Mestre são secretas: vão só para o grupo dos Mestres, nunca para os jogadores.
    /// </summary>
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
        private static string GrupoDosMestres(int campanhaId) => $"Mesa_{campanhaId}_Mestres";

        private async Task<PapelCampanha> ExigirMembroAsync(int campanhaId) =>
            await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId) ?? throw new HubException("Campanha não encontrada.");

        /// <summary>Entra na mesa da campanha e devolve as últimas rolagens.</summary>
        public async Task<IReadOnlyList<RolagemMesa>> EntrarNaMesa(int campanhaId)
        {
            var mestre = await ExigirMembroAsync(campanhaId) == PapelCampanha.Mestre;
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(campanhaId));
            if (mestre) await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDosMestres(campanhaId));
            return _historico.Recentes(campanhaId, incluirSecretas: mestre);
        }

        public async Task SairDaMesa(int campanhaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(campanhaId));
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoDosMestres(campanhaId));
        }

        public async Task Rolar(int campanhaId, RolagemEntradaDto entrada)
        {
            var secreta = await ExigirMembroAsync(campanhaId) == PapelCampanha.Mestre;

            RolagemMesa rolagem;
            try
            {
                rolagem = RolagemMesaFactory.Criar(UsuarioId!.Value, NomeUsuario, entrada, secreta: secreta);
            }
            catch (RegraNegocioException ex)
            {
                throw new HubException(ex.Message);
            }

            _historico.Adicionar(campanhaId, rolagem);
            var destino = secreta ? GrupoDosMestres(campanhaId) : Grupo(campanhaId);
            await Clients.Group(destino).SendAsync("ReceberRolagem", rolagem);
        }
    }
}
