using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories.Anotacao
{
    public class AnotacaoRepository : IAnotacaoRepository
    {
        private readonly WyvernDbContext _context;

        public AnotacaoRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Domain.Entities.Anotacao>> GetAnotacoesByCampanhaAsync(int campanhaId)
        {
            return await _context.Anotacoes
                .Where(a => a.CampanhaId == campanhaId)
                .OrderByDescending(a => a.CriadoEm)
                .ToListAsync();
        }

        public async Task<Wyvern.Domain.Entities.Anotacao?> GetAnotacaoAsync(int id)
        {
            return await _context.Anotacoes.FindAsync(id);
        }

        public async Task CreateAnotacaoAsync(Wyvern.Domain.Entities.Anotacao anotacao)
        {
            _context.Anotacoes.Add(anotacao);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAnotacaoAsync(Wyvern.Domain.Entities.Anotacao anotacao)
        {
            _context.Anotacoes.Update(anotacao);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAnotacaoAsync(int id)
        {
            var anotacao = await _context.Anotacoes.FindAsync(id);
            if (anotacao != null)
            {
                _context.Anotacoes.Remove(anotacao);
                await _context.SaveChangesAsync();
            }
        }
    }
}
