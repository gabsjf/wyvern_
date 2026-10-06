using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wyvern.Domain.Interfaces.Repositories.PastaAnotacao
{
    public interface IPastaAnotacaoRepository
    {
        Task<IEnumerable<Domain.Entities.PastaAnotacao>> GetPastasByCampanhaAsync(int campanhaId);
        Task<Domain.Entities.PastaAnotacao?> GetPastaAsync(int id);
        Task CreatePastaAsync(Domain.Entities.PastaAnotacao pasta);
        Task UpdatePastaAsync(Domain.Entities.PastaAnotacao pasta);
        Task DeletePastaAsync(int id);
    }
}
