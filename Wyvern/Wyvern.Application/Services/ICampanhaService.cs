using Wyvern.Application.DTOs.Campanha;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>Casos de uso de participação numa campanha: convites, membros e papéis.</summary>
    public interface ICampanhaService
    {
        /// <summary>Devolve o convite vigente (criando um se não houver ou se expirou). "renovar" troca o link.</summary>
        Task<ConviteDto> GerarConviteAsync(int campanhaId, int? usuarioId, bool renovar);

        Task RevogarConviteAsync(int campanhaId, int? usuarioId);

        /// <summary>Entra na campanha como Jogador. Devolve o id da campanha; repetir a chamada não duplica a participação.</summary>
        Task<int> EntrarPorConviteAsync(string token, int? usuarioId);

        Task<IReadOnlyList<MembroDto>> ListarMembrosAsync(int campanhaId, int? usuarioId);

        Task AlterarPapelAsync(int campanhaId, int alvoUsuarioId, PapelCampanha novoPapel, int? usuarioId);

        /// <summary>O Mestre remove um membro, ou o próprio usuário sai da campanha.</summary>
        Task RemoverMembroAsync(int campanhaId, int alvoUsuarioId, int? usuarioId);
    }
}
