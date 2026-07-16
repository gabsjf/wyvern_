using Microsoft.EntityFrameworkCore;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;
using Wyvern.Infrastructure.Data;
using Wyvern.Domain.Interfaces;

namespace Wyvern.Infrastructure.Repositories.Campanha
{
    public class CampanhaRepository : ICampanhaRepository
    {
        private readonly WyvernDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CampanhaRepository(WyvernDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<CampanhaEntity>> GetCampanhasAsync()
        {
            return await _context.Campanhas
                .Include(c => c.Mestre)
                .Include(c => c.Sessoes)
                .Where(c => c.Ativo && c.MestreId == _currentUser.UserId)
                .ToListAsync();
        }

        public async Task<CampanhaEntity?> GetCampanhaAsync(int id)
        {
            return await _context.Campanhas
                .Include(c => c.Mestre)
                .Include(c => c.Sessoes)
                .FirstOrDefaultAsync(c => c.CampanhaId == id && c.Ativo && c.MestreId == _currentUser.UserId);
        }

        public async Task<CampanhaEntity> CreateCampanhaAsync(CampanhaEntity campanha)
        {
            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            if (_currentUser.UserId.HasValue)
            {
                campanha.MestreId = _currentUser.UserId.Value;
            }

            _context.Campanhas.Add(campanha);
            await _context.SaveChangesAsync();

            return campanha;
        }

        public async Task<CampanhaEntity> UpdateCampanhaAsync(CampanhaEntity campanha)
        {
            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            _context.Entry(campanha).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return campanha;
        }

        public async Task<CampanhaEntity> DeleteCampanhaAsync(int id)
        {
            var campanha = await _context.Campanhas.FindAsync(id);

            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            campanha.Ativo = false;
            await _context.SaveChangesAsync();

            return campanha;
        }
    }
}