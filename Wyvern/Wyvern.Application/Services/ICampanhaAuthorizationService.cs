using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public interface ICampanhaAuthorizationService
    {
        bool IsMestre(Campanha? campanha, int? userId);
        Task<bool> IsMestreAsync(int campanhaId, int? userId);
    }
}
