using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Data;
using MagiaEntity = Wyvern.Domain.Entities.Magia;
using Wyvern.Domain.Interfaces.Repositories.Magia;


namespace Wyvern.Infrastructure.Repositories.Magia
{
    public class MagiaRepository : IMagiaRepository
    {
        private readonly WyvernDbContext _context;
        public MagiaRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public Task<MagiaEntity> CreateMagiaAsync(MagiaEntity magia)
        {
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));

            _context.Magias.Add(magia);
            return Task.FromResult(magia);
        }


        public async Task<MagiaEntity> DeleteMagiaAsync(int id)
        {
            var magia = await _context.Magias.FindAsync(id);
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));
            magia.Ativo = false;
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

        public Task<MagiaEntity> UpdateMagiaAsync(MagiaEntity magia)
        {
            if (magia is null)
                throw new ArgumentNullException(nameof(magia));

            _context.Entry(magia).State = EntityState.Modified;

            return Task.FromResult(magia);
        }
    }
}
