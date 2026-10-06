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

        public bool IsMestre(Campanha? campanha, int? userId)
        {
            return campanha != null && userId.HasValue && campanha.MestreId == userId.Value;
        }

        public async Task<bool> IsMestreAsync(int campanhaId, int? userId)
        {
            if (!userId.HasValue) return false;

            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(campanhaId);
            return IsMestre(campanha, userId);
        }
    }
}
