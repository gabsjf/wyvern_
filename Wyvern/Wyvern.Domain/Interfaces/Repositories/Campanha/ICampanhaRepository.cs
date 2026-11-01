using System;
using System.Collections.Generic;
using System.Text;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;

namespace Wyvern.Domain.Interfaces.Repositories.Campanha
{
   public interface ICampanhaRepository
    {
        Task<IEnumerable<CampanhaEntity>> GetCampanhasAsync();
        Task<CampanhaEntity?> GetCampanhaAsync(int id);
        Task<CampanhaEntity> CreateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity?> GetCampanhaByTokenAsync(string token);
        /// <summary>Busca a campanha ativa sem filtrar pelo usuário atual; usar só para decisões de autorização.</summary>
        Task<CampanhaEntity?> GetCampanhaParaAutorizacaoAsync(int id);
        Task<CampanhaEntity> UpdateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity> DeleteCampanhaAsync(int id);
    }
}
