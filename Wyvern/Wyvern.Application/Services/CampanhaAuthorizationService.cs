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

        public bool IsMembro(Campanha? campanha, int? userId)
        {
            if (campanha == null || !userId.HasValue) return false;

            return campanha.MestreId == userId.Value
                || (campanha.Jogadores?.Any(j => j.UsuarioId == userId.Value) ?? false);
        }

        public async Task<bool> IsMestreAsync(int campanhaId, int? userId)
        {
            if (!userId.HasValue) return false;

            var campanha = await _uof.CampanhaRepository.GetCampanhaParaAutorizacaoAsync(campanhaId);
            return IsMestre(campanha, userId);
        }

        public async Task<bool> IsMembroAsync(int campanhaId, int? userId)
        {
            if (!userId.HasValue) return false;

            var campanha = await _uof.CampanhaRepository.GetCampanhaParaAutorizacaoAsync(campanhaId);
            return IsMembro(campanha, userId);
        }
    }
}
