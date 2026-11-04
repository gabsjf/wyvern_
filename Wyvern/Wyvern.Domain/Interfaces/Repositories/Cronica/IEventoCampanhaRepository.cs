using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Interfaces.Repositories.Cronica
{
    public interface IEventoCampanhaRepository
    {
        /// <summary>Todos os eventos da campanha, na ordem da cronologia. Inclui autor e sessão.</summary>
        Task<List<EventoCampanha>> GetPorCampanhaAsync(int campanhaId);

        Task<EventoCampanha?> GetAsync(int campanhaId, int eventoId);

        /// <summary>Maior posição já usada na cronologia da campanha (0 se não há eventos).</summary>
        Task<int> MaiorOrdemAsync(int campanhaId);

        Task AddAsync(EventoCampanha evento);
        Task RemoveAsync(EventoCampanha evento);
    }
}
