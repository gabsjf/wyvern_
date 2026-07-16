using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using PericiaEntity = Wyvern.Domain.Entities.Pericia;

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

        public async Task<PericiaEntity> CreatePericiaAsync(PericiaEntity pericia)
        {
            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            _context.Pericias.Add(pericia);
            await _context.SaveChangesAsync();

            return pericia;
        }

        public async Task<PericiaEntity> UpdatePericiaAsync(PericiaEntity pericia)
        {
            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            _context.Entry(pericia).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return pericia;
        }

        public async Task<PericiaEntity> DeletePericiaAsync(int id)
        {
            var pericia = await _context.Pericias.FindAsync(id);

            if (pericia is null)
                throw new ArgumentNullException(nameof(pericia));

            pericia.Ativo = false;
            await _context.SaveChangesAsync();

            return pericia;
        }
    }
}
