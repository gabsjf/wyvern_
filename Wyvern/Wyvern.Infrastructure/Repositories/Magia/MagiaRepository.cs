using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Data;
using MagiaEntity = Wyvern.Domain.Entities.Magia;


namespace Wyvern.Infrastructure.Repositories.Magia
{
    public class MagiaRepository : IMagiaRepository
    {
        private readonly WyvernDbContext _context;
        public MagiaRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<MagiaEntity> CreateMagiaAsync(MagiaEntity magia)
        {
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));

            _context.Magias.Add(magia);
            await _context.SaveChangesAsync();
            return magia;
        }

        
        public async Task<MagiaEntity> DeleteMagiaAsync(int id)
        {
            var magia = await _context.Magias.FindAsync(id);
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));
            magia.Ativo = false;
            await _context.SaveChangesAsync();
            return magia;
        }

        
        public async Task<MagiaEntity?> GetMagiaByIdAsync(int id)
        {
            return await _context.Magias.FirstOrDefaultAsync(m => m.MagiaId == id && m.Ativo);
        }

        public async Task<IEnumerable<MagiaEntity>> GetMagiasAsync()
        {
            return await _context.Magias
                    .Where(m => m.Ativo)
                    .ToListAsync();
        }

        public async Task<MagiaEntity> UpdateMagiaAsync(MagiaEntity magia)
        {
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));

            _context.Entry(magia).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return magia;
        }
    }
}
