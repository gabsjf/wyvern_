using System;
using System.Collections.Generic;
using System.Text;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;

namespace Wyvern.Infrastructure.Repositories.Campanha
{
   public interface ICampanhaRepository
    {
        Task<IEnumerable<CampanhaEntity>> GetCampanhasAsync();
        Task<CampanhaEntity?> GetCampanhaAsync(int id);
        Task<CampanhaEntity> CreateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity> UpdateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity> DeleteCampanhaAsync(int id);
    }
}
