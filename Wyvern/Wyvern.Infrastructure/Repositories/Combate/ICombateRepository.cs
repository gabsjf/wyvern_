using System.Collections.Generic;
using System.Threading.Tasks;
using CombateEntity = Wyvern.Domain.Entities.Combate;
using Wyvern.Domain.Entities;

namespace Wyvern.Infrastructure.Repositories.Combate
{
    public interface ICombateRepository
    {
        Task<IEnumerable<CombateEntity>> GetCombatesAsync(int sessaoId);
        Task<CombateEntity?> GetCombateAsync(int id);
        Task<CombateEntity?> GetActiveCombateByCampanhaAsync(int campanhaId);
        Task<CombateEntity?> GetActiveCombateBySessaoAsync(int sessaoId);
        Task CreateCombateAsync(CombateEntity combate);
        Task<CombateEntity> UpdateCombateAsync(CombateEntity combate);
        Task<CombateEntity> DeleteCombateAsync(int id);
        Task<CombateParticipante> UpdateParticipanteAsync(CombateParticipante participante);
    }
}
