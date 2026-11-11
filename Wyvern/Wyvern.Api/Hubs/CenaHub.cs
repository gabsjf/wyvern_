using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Hubs
{
    /// <summary>
    /// O mapa ao vivo de uma campanha. Dois grupos por campanha: todos os membros, e só o Mestre (para o que os
    /// jogadores não podem ver, como tokens ocultos). Criar, editar e apagar passa pela API REST, que avisa o grupo;
    /// mover passa por aqui, pela latência.
    /// </summary>
    [Authorize]
    public class CenaHub : Hub
    {
        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;

        public CenaHub(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
        }

        private int? UsuarioId =>
            int.TryParse(Context.User?.FindFirst("nameid")?.Value, out var id) ? id : null;

        public static string GrupoDaMesa(int campanhaId) => $"Cena_{campanhaId}";
        public static string GrupoDoMestre(int campanhaId) => $"CenaMestre_{campanhaId}";

        public async Task EntrarNaCena(int campanhaId)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId);
            if (papel == null) throw new HubException("Campanha não encontrada.");

            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDaMesa(campanhaId));
            if (papel == PapelCampanha.Mestre)
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDoMestre(campanhaId));
        }

        public async Task SairDaCena(int campanhaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoDaMesa(campanhaId));
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoDoMestre(campanhaId));
        }

        /// <summary>O Mestre revela ou cobre um retângulo de quadrados. Vale para todos, na hora.</summary>
        public async Task AlterarNeblina(int campanhaId, int cenaId, int coluna, int linha, int largura, int altura, bool revelar)
        {
            if (await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId) != PapelCampanha.Mestre)
                throw new HubException("Apenas o Mestre mexe na névoa.");

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            var imagem = cena == null ? null : await _uof.CenaRepository.GetImagemParaEditarAsync(cenaId);
            if (cena == null || imagem == null) throw new HubException("Cena não encontrada.");

            var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
            imagem.Neblina = NeblinaRegras.Aplicar(imagem.Neblina, colunas, linhas, coluna, linha, largura, altura, revelar);
            await _uof.CommitAsync();

            await Clients.Group(GrupoDaMesa(campanhaId)).SendAsync("NeblinaAlterada", new { cenaId, coluna, linha, largura, altura, revelar });
        }

        /// <summary>Um "olhem aqui": aparece para todos no ponto do mapa (em pixels da imagem) por alguns segundos.</summary>
        public async Task Ping(int campanhaId, int cenaId, double x, double y)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId);
            if (papel == null || UsuarioId is not { } usuarioId) throw new HubException("Campanha não encontrada.");
            if (!double.IsFinite(x) || !double.IsFinite(y)) throw new HubException("Ponto inválido.");

            // no máximo um ping a cada meio segundo por conexão
            var agora = DateTime.UtcNow;
            if (Context.Items.TryGetValue("ultimoPing", out var anterior) && anterior is DateTime t && agora - t < TimeSpan.FromMilliseconds(500))
                return;
            Context.Items["ultimoPing"] = agora;

            var nome = Context.User?.FindFirst("unique_name")?.Value ?? "Jogador";
            await Clients.Group(GrupoDaMesa(campanhaId)).SendAsync("Ping", new
            {
                usuarioId, nome, cenaId, x = Math.Clamp(x, 0, 20000), y = Math.Clamp(y, 0, 20000),
            });
        }

        /// <summary>A régua de quem mede aparece, ao vivo, para os outros da mesa (nada é gravado).</summary>
        public async Task Regua(int campanhaId, int cenaId, int c1, int l1, int c2, int l2, bool ativa)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId);
            if (papel == null || UsuarioId is not { } usuarioId) throw new HubException("Campanha não encontrada.");

            // medir é rápido: no máximo uma atualização a cada 40 ms por conexão (o "desligar" sempre passa)
            var agora = DateTime.UtcNow;
            if (ativa && Context.Items.TryGetValue("ultimaRegua", out var anterior) && anterior is DateTime t && agora - t < TimeSpan.FromMilliseconds(40))
                return;
            Context.Items["ultimaRegua"] = agora;

            var nome = Context.User?.FindFirst("unique_name")?.Value ?? "Jogador";
            await Clients.OthersInGroup(GrupoDaMesa(campanhaId)).SendAsync("ReguaCompartilhada", new
            {
                usuarioId, nome, cenaId, c1 = Math.Clamp(c1, 0, 1000), l1 = Math.Clamp(l1, 0, 1000), c2 = Math.Clamp(c2, 0, 1000), l2 = Math.Clamp(l2, 0, 1000), ativa,
            });
        }

        /// <summary>Move o token para um quadrado. O servidor confere quem pode e ajusta ao que cabe no mapa.</summary>
        public async Task MoverToken(int campanhaId, int tokenId, int coluna, int linha)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, UsuarioId);
            if (papel == null || UsuarioId is not { } usuarioId) throw new HubException("Campanha não encontrada.");

            var token = await _uof.CenaRepository.GetTokenDaCampanhaAsync(campanhaId, tokenId);
            if (token?.Cena == null || (papel != PapelCampanha.Mestre && token.Oculto))
                throw new HubException("Token não encontrado.");
            if (!CenaRegras.PodeMover(token, papel.Value, usuarioId))
                throw new HubException("Você não pode mover este token.");

            var cena = token.Cena;
            var mestre = papel == PapelCampanha.Mestre;
            var destino = CenaRegras.Limitar(cena, token.Tamanho, coluna, linha);

            // o combate a que o token pertence (a campanha pode ter um por sessão): dele vêm a vez e a rodada
            Combate? combate = null;
            if (token.ParticipanteId is { } participanteId && (cena.SoNoTurno || cena.LimitarDeslocamento))
                combate = await _uof.CombateRepository.GetAtivoDoParticipanteAsync(participanteId);

            if (!mestre && cena.SoNoTurno && !CenaRegras.NaVez(cena, token, combate))
                throw new HubException("Não é a vez deste token.");

            // deslocamento da ficha (ou o escrito no token): só conta dentro de um combate, por turno
            var porQuadrado = MovimentoRegras.MetrosPorQuadrado(cena);
            var metros = token.DeslocamentoMetros ?? MovimentoRegras.DaFicha(token.Personagem);
            var controla = cena.LimitarDeslocamento && combate != null && metros != null;
            var carimbo = combate?.CarimboDoTurno();
            var gasto = controla && token.MovimentoTurno == carimbo ? token.MovimentoGasto : 0;
            var restante = controla ? Math.Max(0, MovimentoRegras.Quadrados(metros!.Value, porQuadrado) - gasto) : int.MaxValue;

            int? custo;
            if (mestre)
            {
                // o Mestre vai aonde quiser, mas o que anda em combate também conta para o turno
                custo = controla
                    ? MovimentoRegras.Custo(cena, token.Tamanho, token.Coluna, token.Linha, destino.coluna, destino.linha, await _uof.CenaRepository.GetParedesAsync(token.CenaId))
                        ?? Math.Max(Math.Abs(destino.coluna - token.Coluna), Math.Abs(destino.linha - token.Linha))
                    : null;
            }
            else
            {
                var paredes = await _uof.CenaRepository.GetParedesAsync(token.CenaId);
                custo = MovimentoRegras.Custo(cena, token.Tamanho, token.Coluna, token.Linha, destino.coluna, destino.linha, paredes, restante);
                if (custo == null)
                {
                    // existe caminho, mas é mais longo que o que resta? ou não existe caminho nenhum?
                    var existe = restante != int.MaxValue
                        && MovimentoRegras.Custo(cena, token.Tamanho, token.Coluna, token.Linha, destino.coluna, destino.linha, paredes) != null;
                    throw new HubException(existe
                        ? $"Deslocamento insuficiente: restam {restante} quadrado(s) ({restante * porQuadrado:0.##} m)."
                        : "Há uma parede no caminho.");
                }
            }

            (token.Coluna, token.Linha) = destino;
            if (controla && custo is > 0)
            {
                token.MovimentoTurno = carimbo;
                token.MovimentoGasto = gasto + custo.Value;
            }
            await _uof.CommitAsync();

            var grupo = token.Oculto ? GrupoDoMestre(campanhaId) : GrupoDaMesa(campanhaId);
            await Clients.Group(grupo).SendAsync("TokenMovido", new { token.TokenId, token.CenaId, token.Coluna, token.Linha, token.MovimentoGasto, token.MovimentoTurno });
        }
    }
}
