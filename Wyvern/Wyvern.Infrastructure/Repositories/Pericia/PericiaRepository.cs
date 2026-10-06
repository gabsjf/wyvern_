using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using PericiaEntity = Wyvern.Domain.Entities.Pericia;
using Wyvern.Domain.Interfaces.Repositories.Pericia;

namespace Wyvern.Infrastructure.Repositories.Pericia
{
    public class PericiaRepository : IPericiaRepository
    {
        private readonly WyvernDbContext _context;

        public PericiaRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PericiaEntity>> GetPericiasAsync()
        {
            return await _context.Pericias
                .Where(p => p.Ativo)
                .ToListAsync();
        }

        public async Task<PericiaEntity?> GetPericiaAsync(int id)
        {
            return await _context.Pericias.FirstOrDefaultAsync(p => p.PericiaId == id && p.Ativo);
        }

        public Task<PericiaEntity> CreatePericiaAsync(PericiaEntity pericia)
        {
            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            _context.Pericias.Add(pericia);

            return Task.FromResult(pericia);
        }

        public Task<PericiaEntity> UpdatePericiaAsync(PericiaEntity pericia)
        {
            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            _context.Entry(pericia).State = EntityState.Modified;

            return Task.FromResult(pericia);
        }

        public async Task<PericiaEntity> DeletePericiaAsync(int id)
        {
            var pericia = await _context.Pericias.FindAsync(id);

            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            pericia.Ativo = false;

            return pericia;
        }
    }
}
