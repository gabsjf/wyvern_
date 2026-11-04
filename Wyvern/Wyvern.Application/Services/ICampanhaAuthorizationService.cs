using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Fonte única das decisões de acesso a uma campanha, sempre pelo papel que o usuário tem NAQUELA campanha
    /// (o papel global do cadastro não concede nada).
    /// </summary>
    public interface ICampanhaAuthorizationService
    {
        /// <summary>Papel do usuário na campanha, ou nulo se a campanha não existe ou ele não participa.</summary>
        Task<PapelCampanha?> ObterPapelAsync(int campanhaId, int? userId);
        Task<bool> IsMestreAsync(int campanhaId, int? userId);
        /// <summary>Mestre ou jogador da campanha.</summary>
        Task<bool> IsMembroAsync(int campanhaId, int? userId);
    }
}
