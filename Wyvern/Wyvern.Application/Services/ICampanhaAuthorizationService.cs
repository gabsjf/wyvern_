using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public interface ICampanhaAuthorizationService
    {
        bool IsMestre(Campanha? campanha, int? userId);
        bool IsMembro(Campanha? campanha, int? userId);
        Task<bool> IsMestreAsync(int campanhaId, int? userId);
        /// <summary>Mestre ou jogador da campanha.</summary>
        Task<bool> IsMembroAsync(int campanhaId, int? userId);
    }
}
