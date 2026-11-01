using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    public class CampanhaAuthorizationService : ICampanhaAuthorizationService
    {
        private readonly IUnitOfWork _uof;

        public CampanhaAuthorizationService(IUnitOfWork uof)
        {
            _uof = uof;
        }

        public async Task<PapelCampanha?> ObterPapelAsync(int campanhaId, int? userId)
        {
            if (!userId.HasValue) return null;

            return await _uof.CampanhaRepository.ObterPapelAsync(campanhaId, userId.Value);
        }

        public async Task<bool> IsMestreAsync(int campanhaId, int? userId)
        {
            return await ObterPapelAsync(campanhaId, userId) == PapelCampanha.Mestre;
        }

        public async Task<bool> IsMembroAsync(int campanhaId, int? userId)
        {
            return await ObterPapelAsync(campanhaId, userId) != null;
        }
    }
}
