using System.Collections.Generic;
using System.Threading.Tasks;
using Wyvern.Domain.Entities;

namespace Wyvern.Infrastructure.Repositories.Anotacao
{
    public interface IAnotacaoRepository
    {
        Task<IEnumerable<Domain.Entities.Anotacao>> GetAnotacoesByCampanhaAsync(int campanhaId);
        Task<Wyvern.Domain.Entities.Anotacao?> GetAnotacaoAsync(int id);
        Task CreateAnotacaoAsync(Wyvern.Domain.Entities.Anotacao anotacao);
        Task UpdateAnotacaoAsync(Wyvern.Domain.Entities.Anotacao anotacao);
        Task DeleteAnotacaoAsync(int id);
    }
}
