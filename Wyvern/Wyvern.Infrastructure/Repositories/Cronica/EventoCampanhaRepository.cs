using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories.Cronica;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories.Cronica
{
    public class EventoCampanhaRepository : IEventoCampanhaRepository
    {
        private readonly WyvernDbContext _context;

        public EventoCampanhaRepository(WyvernDbContext context) => _context = context;

        public Task<List<EventoCampanha>> GetPorCampanhaAsync(int campanhaId) =>
            _context.EventosCampanha
                .Include(e => e.CriadoPor)
                .Include(e => e.Sessao)
                .Where(e => e.CampanhaId == campanhaId)
                .OrderBy(e => e.Ordem).ThenBy(e => e.EventoId)
                .ToListAsync();

        public Task<EventoCampanha?> GetAsync(int campanhaId, int eventoId) =>
            _context.EventosCampanha
                .Include(e => e.CriadoPor)
                .Include(e => e.Sessao)
                .FirstOrDefaultAsync(e => e.CampanhaId == campanhaId && e.EventoId == eventoId);

        public async Task<int> MaiorOrdemAsync(int campanhaId) =>
            await _context.EventosCampanha.Where(e => e.CampanhaId == campanhaId).Select(e => (int?)e.Ordem).MaxAsync() ?? 0;

        public Task AddAsync(EventoCampanha evento)
        {
            _context.EventosCampanha.Add(evento);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(EventoCampanha evento)
        {
            _context.EventosCampanha.Remove(evento);
            return Task.CompletedTask;
        }
    }
}
