using Microsoft.AspNetCore.SignalR;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Hubs
{
    /// <summary>
    /// Avisos de token do mapa ao vivo, com a névoa valendo no servidor: o Mestre recebe tudo; os jogadores, só os tokens
    /// que enxergam (<see cref="VisaoRegras"/>). Quando a visibilidade muda (alguém anda, a névoa ou uma parede muda),
    /// os jogadores recebem o token que apareceu ("TokenAtualizado") e perdem o que sumiu ("TokenRemovido").
    /// </summary>
    public static class AvisosDoMapa
    {
        /// <summary>Os tokens que os jogadores da cena enxergam agora (lido do banco: chame depois do Commit).</summary>
        public static async Task<HashSet<int>> VisiveisAsync(IUnitOfWork uof, Cena cena)
        {
            var (visiveis, _) = await CalcularAsync(uof, cena);
            return visiveis;
        }

        private static async Task<(HashSet<int> visiveis, List<TokenCena> tokens)> CalcularAsync(IUnitOfWork uof, Cena cena)
        {
            var tokens = await uof.CenaRepository.GetTokensAsync(cena.CenaId);
            if (!cena.Ativa) return ([], tokens);
            var paredes = cena.NeblinaAtiva ? await uof.CenaRepository.GetParedesAsync(cena.CenaId) : [];
            var neblina = cena.NeblinaAtiva ? await uof.CenaRepository.GetNeblinaAsync(cena.CenaId) : null;
            return (VisaoRegras.TokensVisiveisAosJogadores(cena, tokens, paredes, neblina), tokens);
        }

        /// <summary>
        /// Depois de algo que muda a visão (névoa, paredes, tokens): manda aos jogadores o que entrou e o que saiu da vista
        /// desde <paramref name="antes"/>. Devolve o que eles enxergam agora.
        /// </summary>
        public static async Task<HashSet<int>> AvisarVisibilidadeAsync(this IHubClients<IClientProxy> clientes, IUnitOfWork uof, Cena cena, HashSet<int> antes)
        {
            var (depois, tokens) = await CalcularAsync(uof, cena);
            var jogadores = clientes.Group(CenaHub.GrupoDosJogadores(cena.CampanhaId));
            foreach (var t in tokens.Where(t => depois.Contains(t.TokenId) && !antes.Contains(t.TokenId)))
                await jogadores.SendAsync("TokenAtualizado", TokenCenaDto.De(t));
            foreach (var tokenId in antes.Where(id => !depois.Contains(id)))
                await jogadores.SendAsync("TokenRemovido", new { tokenId, cenaId = cena.CenaId });
            return depois;
        }

        /// <summary>
        /// Um token mudou (criado, editado, movido, girado). O Mestre recebe o evento; os jogadores, só se enxergavam o
        /// token antes e depois (quem aparece agora chega inteiro pela visibilidade, quem sumiu sai dela).
        /// </summary>
        public static async Task AvisarTokenAsync(this IHubClients<IClientProxy> clientes, IUnitOfWork uof, Cena cena, HashSet<int> antes,
            int tokenId, string evento, object carga)
        {
            await clientes.Group(CenaHub.GrupoDoMestre(cena.CampanhaId)).SendAsync(evento, carga);
            var depois = await clientes.AvisarVisibilidadeAsync(uof, cena, antes);
            if (antes.Contains(tokenId) && depois.Contains(tokenId))
                await clientes.Group(CenaHub.GrupoDosJogadores(cena.CampanhaId)).SendAsync(evento, carga);
        }
    }
}
