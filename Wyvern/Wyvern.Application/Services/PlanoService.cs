using Microsoft.Extensions.Options;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    /// <summary>Limites de um plano. Nulo = sem limite.</summary>
    public class PlanoConfig
    {
        public string Nome { get; set; } = string.Empty;
        public int? MaxCampanhas { get; set; }
        /// <summary>Jogadores por campanha além do Mestre dono.</summary>
        public int? MaxJogadoresPorCampanha { get; set; }
    }

    /// <summary>
    /// Seção "Planos" da configuração. Os valores daqui são o padrão; a configuração sobrescreve chave por chave,
    /// então dá para mudar nome e limites sem mexer no código. A chave ("Free", "Standard"...) é o que fica no banco.
    /// </summary>
    public class PlanosOptions
    {
        public const string Secao = "Planos";
        public const string Gratuito = "Free";

        /// <summary>Durante o beta toda conta tem acesso completo, sem limites de plano.</summary>
        public bool Beta { get; set; } = true;

        public Dictionary<string, PlanoConfig> Catalogo { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            [Gratuito] = new() { Nome = "Free", MaxCampanhas = 1, MaxJogadoresPorCampanha = 2 },
            ["Standard"] = new() { Nome = "Standard", MaxCampanhas = 3, MaxJogadoresPorCampanha = 6 },
            ["Vip"] = new() { Nome = "VIP", MaxCampanhas = 10, MaxJogadoresPorCampanha = 10 },
            ["Ultra"] = new() { Nome = "Ultra" },
        };
    }

    public interface IPlanoService
    {
        CatalogoDePlanosDto Catalogo();
        Task<ContaDto> ContaAsync(int usuarioId);

        /// <summary>Plano sem vaga (fora do beta): AcessoNegadoException com o motivo.</summary>
        Task GarantirPodeCriarCampanhaAsync(int usuarioId);

        /// <summary>Usa o plano do dono da campanha; mesa cheia: RegraNegocioException.</summary>
        Task GarantirVagaNaCampanhaAsync(Campanha campanha);

        /// <summary>Admin hoje, pagamento no futuro. Chave fora do catálogo: RegraNegocioException.</summary>
        Task<ContaDto> DefinirPlanoAsync(int usuarioId, string plano);
    }

    public class PlanoService : IPlanoService
    {
        private readonly IUnitOfWork _uof;
        private readonly PlanosOptions _opcoes;

        public PlanoService(IUnitOfWork uof, IOptions<PlanosOptions> opcoes)
        {
            _uof = uof;
            _opcoes = opcoes.Value;
        }

        public CatalogoDePlanosDto Catalogo() => new()
        {
            Beta = _opcoes.Beta,
            Planos = _opcoes.Catalogo
                .Select(p => new PlanoPublicoDto { Chave = p.Key, Nome = p.Value.Nome, MaxCampanhas = p.Value.MaxCampanhas, MaxJogadoresPorCampanha = p.Value.MaxJogadoresPorCampanha })
                .ToList()
        };

        public async Task<ContaDto> ContaAsync(int usuarioId)
        {
            var user = await _uof.UsuarioRepository.GetResumoAsync(usuarioId)
                       ?? throw new NaoEncontradoException("Conta não encontrada.");
            var criadas = await _uof.CampanhaRepository.ContarCampanhasDoDonoAsync(usuarioId);
            return Montar(user, criadas);
        }

        public async Task GarantirPodeCriarCampanhaAsync(int usuarioId)
        {
            var conta = await ContaAsync(usuarioId);
            if (!conta.PodeCriarCampanha)
                throw new AcessoNegadoException(conta.MotivoBloqueio ?? "Você não pode criar campanhas agora.");
        }

        public async Task GarantirVagaNaCampanhaAsync(Campanha campanha)
        {
            if (_opcoes.Beta) return;

            var dono = await _uof.UsuarioRepository.GetResumoAsync(campanha.MestreId);
            var (_, plano) = PlanoDe(dono);
            if (plano.MaxJogadoresPorCampanha is not { } maximo) return;

            // Conta todo mundo menos o dono: promover alguém a co-Mestre não abre vaga.
            var ocupadas = campanha.Membros?.Count(m => m.UsuarioId != campanha.MestreId) ?? 0;
            if (ocupadas >= maximo)
                throw new RegraNegocioException(
                    $"Esta campanha está cheia: o plano do Mestre permite até {maximo} {(maximo == 1 ? "jogador" : "jogadores")}. Peça para o Mestre liberar uma vaga.");
        }

        public async Task<ContaDto> DefinirPlanoAsync(int usuarioId, string plano)
        {
            var chave = _opcoes.Catalogo.Keys.FirstOrDefault(k => string.Equals(k, plano?.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? throw new RegraNegocioException($"Plano desconhecido. Use um destes: {string.Join(", ", _opcoes.Catalogo.Keys)}.");

            var user = await UsuarioParaEditarAsync(usuarioId);
            user.Plano = chave;
            user.PlanoAlteradoEm = DateTime.UtcNow;
            await _uof.CommitAsync();
            return await ContaAsync(usuarioId);
        }

        private async Task<Usuario> UsuarioParaEditarAsync(int usuarioId) =>
            await _uof.UsuarioRepository.GetUsuarioAsync(usuarioId) ?? throw new NaoEncontradoException("Conta não encontrada.");

        // Plano gravado fora do catálogo (renomeado na configuração, coluna vazia num banco antigo): vale o gratuito.
        private (string chave, PlanoConfig plano) PlanoDe(Usuario? user)
        {
            if (user != null && !string.IsNullOrEmpty(user.Plano) && _opcoes.Catalogo.TryGetValue(user.Plano, out var plano))
                return (_opcoes.Catalogo.Keys.First(k => string.Equals(k, user.Plano, StringComparison.OrdinalIgnoreCase)), plano);

            return _opcoes.Catalogo.TryGetValue(PlanosOptions.Gratuito, out var gratuito)
                ? (PlanosOptions.Gratuito, gratuito)
                : (PlanosOptions.Gratuito, new PlanoConfig { Nome = "Free", MaxCampanhas = 1, MaxJogadoresPorCampanha = 2 });
        }

        private ContaDto Montar(Usuario user, int criadas)
        {
            var (chave, plano) = PlanoDe(user);
            var conta = new ContaDto
            {
                Plano = chave,
                NomePlano = plano.Nome,
                Beta = _opcoes.Beta,
                CampanhasCriadas = criadas
            };

            if (_opcoes.Beta)
            {
                conta.PodeCriarCampanha = true; // sem limites no beta
                return conta;
            }

            conta.MaxCampanhas = plano.MaxCampanhas;
            conta.MaxJogadoresPorCampanha = plano.MaxJogadoresPorCampanha;
            conta.PodeCriarCampanha = plano.MaxCampanhas is not { } maximo || criadas < maximo;
            if (!conta.PodeCriarCampanha)
                conta.MotivoBloqueio = $"O plano {plano.Nome} permite {plano.MaxCampanhas} {(plano.MaxCampanhas == 1 ? "campanha" : "campanhas")}. Encerre uma campanha ou mude de plano para criar outra.";
            return conta;
        }
    }
}
