using Microsoft.Extensions.Configuration;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    public class CampanhaService : ICampanhaService
    {
        private const int ValidadePadraoDias = 14;

        private readonly IUnitOfWork _uof;
        private readonly TimeProvider _tempo;
        private readonly int _validadeDias;

        public CampanhaService(IUnitOfWork uof, TimeProvider tempo, IConfiguration configuration)
        {
            _uof = uof;
            _tempo = tempo;
            _validadeDias = int.TryParse(configuration["Convite:ValidadeDias"], out var dias) && dias > 0 ? dias : ValidadePadraoDias;
        }

        // Carrega a campanha para alteração e garante a permissão. Quem não participa recebe 404
        // (a campanha "não existe" para ele); quem participa sem ser Mestre recebe 403.
        private async Task<(Campanha campanha, CampanhaMembro membro)> CarregarAsync(int campanhaId, int? usuarioId, bool exigeMestre, string acao)
        {
            var campanha = await _uof.CampanhaRepository.GetCampanhaParaGestaoAsync(campanhaId);
            var membro = usuarioId.HasValue ? campanha?.Membros?.FirstOrDefault(m => m.UsuarioId == usuarioId.Value) : null;

            if (campanha == null || membro == null)
                throw new NaoEncontradoException("Campanha não encontrada.");

            if (exigeMestre && membro.Papel != PapelCampanha.Mestre)
                throw new AcessoNegadoException($"Apenas o Mestre pode {acao}.");

            return (campanha, membro);
        }

        public async Task<ConviteDto> GerarConviteAsync(int campanhaId, int? usuarioId, bool renovar)
        {
            var (campanha, _) = await CarregarAsync(campanhaId, usuarioId, exigeMestre: true, acao: "gerar convites");
            var agora = _tempo.GetUtcNow().UtcDateTime;

            var expirado = campanha.ConviteExpiraEm.HasValue && campanha.ConviteExpiraEm.Value <= agora;
            if (renovar || string.IsNullOrEmpty(campanha.TokenConvite) || expirado)
            {
                campanha.TokenConvite = Guid.NewGuid().ToString("N");
                campanha.ConviteExpiraEm = agora.AddDays(_validadeDias);
                await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
                await _uof.CommitAsync();
            }
            else if (!campanha.ConviteExpiraEm.HasValue)
            {
                // Convite anterior à validade: passa a valer pelo prazo padrão, sem trocar o link já enviado.
                campanha.ConviteExpiraEm = agora.AddDays(_validadeDias);
                await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
                await _uof.CommitAsync();
            }

            return new ConviteDto { Token = campanha.TokenConvite!, ExpiraEm = campanha.ConviteExpiraEm };
        }

        public async Task RevogarConviteAsync(int campanhaId, int? usuarioId)
        {
            var (campanha, _) = await CarregarAsync(campanhaId, usuarioId, exigeMestre: true, acao: "revogar convites");

            campanha.TokenConvite = null;
            campanha.ConviteExpiraEm = null;
            await _uof.CampanhaRepository.UpdateCampanhaAsync(campanha);
            await _uof.CommitAsync();
        }

        public async Task<int> EntrarPorConviteAsync(string token, int? usuarioId)
        {
            if (!usuarioId.HasValue)
                throw new AcessoNegadoException("Faça login para entrar na campanha.");

            var campanha = string.IsNullOrWhiteSpace(token) ? null : await _uof.CampanhaRepository.GetCampanhaByTokenAsync(token);
            var agora = _tempo.GetUtcNow().UtcDateTime;

            if (campanha == null || (campanha.ConviteExpiraEm.HasValue && campanha.ConviteExpiraEm.Value <= agora))
                throw new NaoEncontradoException("Convite inválido ou expirado.");

            var existente = campanha.Membros?.FirstOrDefault(m => m.UsuarioId == usuarioId.Value);
            if (existente != null)
            {
                if (existente.Papel == PapelCampanha.Mestre)
                    throw new RegraNegocioException("Você já é Mestre desta campanha.");

                return campanha.CampanhaId; // já participa: nada a fazer
            }

            campanha.Membros ??= new List<CampanhaMembro>();
            campanha.Membros.Add(new CampanhaMembro
            {
                CampanhaId = campanha.CampanhaId,
                UsuarioId = usuarioId.Value,
                Papel = PapelCampanha.Jogador,
                EntrouEm = DateTime.Now
            });
            await _uof.CommitAsync();

            return campanha.CampanhaId;
        }

        public async Task<IReadOnlyList<MembroDto>> ListarMembrosAsync(int campanhaId, int? usuarioId)
        {
            var (campanha, _) = await CarregarAsync(campanhaId, usuarioId, exigeMestre: false, acao: "listar membros");

            return campanha.Membros!
                .OrderBy(m => m.Papel)           // Mestres primeiro
                .ThenBy(m => m.EntrouEm)
                .Select(m => new MembroDto
                {
                    UsuarioId = m.UsuarioId,
                    Nome = m.Usuario?.Nome ?? string.Empty,
                    Papel = m.Papel.ToString(),
                    EntrouEm = m.EntrouEm,
                    EhCriador = m.UsuarioId == campanha.MestreId,
                    EhVoce = m.UsuarioId == usuarioId
                })
                .ToList();
        }

        public async Task AlterarPapelAsync(int campanhaId, int alvoUsuarioId, PapelCampanha novoPapel, int? usuarioId)
        {
            var (campanha, _) = await CarregarAsync(campanhaId, usuarioId, exigeMestre: true, acao: "alterar papéis");

            var alvo = campanha.Membros!.FirstOrDefault(m => m.UsuarioId == alvoUsuarioId)
                ?? throw new NaoEncontradoException("Membro não encontrado nesta campanha.");

            if (alvo.Papel == novoPapel) return;

            if (alvo.Papel == PapelCampanha.Mestre)
                GarantirOutroMestre(campanha, alvo, "rebaixar");

            alvo.Papel = novoPapel;
            await _uof.CommitAsync();
        }

        public async Task RemoverMembroAsync(int campanhaId, int alvoUsuarioId, int? usuarioId)
        {
            var saindo = usuarioId == alvoUsuarioId;
            var (campanha, _) = await CarregarAsync(campanhaId, usuarioId, exigeMestre: !saindo, acao: "remover membros");

            var alvo = campanha.Membros!.FirstOrDefault(m => m.UsuarioId == alvoUsuarioId)
                ?? throw new NaoEncontradoException("Membro não encontrado nesta campanha.");

            if (alvo.Papel == PapelCampanha.Mestre)
                GarantirOutroMestre(campanha, alvo, saindo ? "sair" : "remover");

            campanha.Membros!.Remove(alvo);
            await _uof.CommitAsync();
        }

        // Toda campanha precisa de pelo menos um Mestre.
        private static void GarantirOutroMestre(Campanha campanha, CampanhaMembro alvo, string acao)
        {
            var outrosMestres = campanha.Membros!.Count(m => m.Papel == PapelCampanha.Mestre && m.UsuarioId != alvo.UsuarioId);
            if (outrosMestres == 0)
                throw new RegraNegocioException($"Não é possível {acao}: a campanha precisa de pelo menos um Mestre. Promova outro membro antes.");
        }
    }
}
