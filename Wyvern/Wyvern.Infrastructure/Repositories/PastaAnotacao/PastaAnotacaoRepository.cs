using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories.PastaAnotacao
{
    public class PastaAnotacaoRepository : IPastaAnotacaoRepository
    {
        private readonly WyvernDbContext _context;

        public PastaAnotacaoRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Domain.Entities.PastaAnotacao>> GetPastasByCampanhaAsync(int campanhaId)
        {
            return await _context.PastasAnotacao
                .Where(p => p.CampanhaId == campanhaId)
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }

        public async Task<Domain.Entities.PastaAnotacao?> GetPastaAsync(int id)
        {
            return await _context.PastasAnotacao.FindAsync(id);
        }

        public async Task CreatePastaAsync(Domain.Entities.PastaAnotacao pasta)
        {
            await _context.PastasAnotacao.AddAsync(pasta);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePastaAsync(Domain.Entities.PastaAnotacao pasta)
        {
            _context.PastasAnotacao.Update(pasta);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePastaAsync(int id)
        {
            var pasta = await GetPastaAsync(id);
            if (pasta != null)
            {
                _context.PastasAnotacao.Remove(pasta);
                await _context.SaveChangesAsync();
            }
        }
    }
}
