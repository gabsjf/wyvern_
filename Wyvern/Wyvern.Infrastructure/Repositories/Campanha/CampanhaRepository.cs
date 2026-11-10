using Microsoft.EntityFrameworkCore;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Data;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories.Campanha;

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

        // Só campanhas ativas das quais o usuário atual é membro (Mestre ou Jogador).
        private IQueryable<CampanhaEntity> CampanhasVisiveis()
        {
            var userId = _currentUser.UserId;

            return _context.Campanhas
                .Include(c => c.Mestre)
                .Include(c => c.Sessoes)
                .Include(c => c.Membros)
                .Where(c => c.Ativo && c.Membros!.Any(m => m.UsuarioId == userId));
        }

        public async Task<IEnumerable<CampanhaEntity>> GetCampanhasAsync()
        {
            return await CampanhasVisiveis().ToListAsync();
        }

        public async Task<CampanhaEntity?> GetCampanhaAsync(int id)
        {
            return await CampanhasVisiveis().FirstOrDefaultAsync(c => c.CampanhaId == id);
        }

        public async Task<CampanhaEntity?> GetCampanhaByTokenAsync(string token)
        {
            return await _context.Campanhas
                .Include(c => c.Mestre)
                .Include(c => c.Sessoes)
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.TokenConvite == token && c.Ativo);
        }

        public async Task<PapelCampanha?> ObterPapelAsync(int campanhaId, int usuarioId)
        {
            return await _context.CampanhasMembros
                .AsNoTracking()
                .Where(m => m.CampanhaId == campanhaId && m.UsuarioId == usuarioId && m.Campanha!.Ativo)
                .Select(m => (PapelCampanha?)m.Papel)
                .FirstOrDefaultAsync();
        }

        public async Task<CampanhaEntity?> GetCampanhaParaGestaoAsync(int campanhaId)
        {
            return await _context.Campanhas
                .Include(c => c.Membros!)
                    .ThenInclude(m => m.Usuario)
                .FirstOrDefaultAsync(c => c.CampanhaId == campanhaId && c.Ativo);
        }

        public Task<List<CampanhaEntity>> GetCampanhasDoUsuarioParaGestaoAsync(int usuarioId) =>
            _context.Campanhas
                .Include(c => c.Membros!)
                    .ThenInclude(m => m.Usuario)
                .Where(c => c.Ativo && (c.MestreId == usuarioId || c.Membros!.Any(m => m.UsuarioId == usuarioId)))
                .ToListAsync();

        public Task<int> ContarCampanhasDoDonoAsync(int usuarioId) =>
            _context.Campanhas.CountAsync(c => c.MestreId == usuarioId && c.Ativo);

        public Task<CampanhaEntity> CreateCampanhaAsync(CampanhaEntity campanha)
        {
            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            if (_currentUser.UserId.HasValue)
            {
                campanha.MestreId = _currentUser.UserId.Value;
                campanha.Membros = new List<CampanhaMembro>
                {
                    new CampanhaMembro { UsuarioId = _currentUser.UserId.Value, Papel = PapelCampanha.Mestre }
                };
            }

            _context.Campanhas.Add(campanha);

            return Task.FromResult(campanha);
        }

        public Task<CampanhaEntity> UpdateCampanhaAsync(CampanhaEntity campanha)
        {
            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            _context.Entry(campanha).State = EntityState.Modified;

            return Task.FromResult(campanha);
        }

        public async Task<CampanhaEntity> DeleteCampanhaAsync(int id)
        {
            var campanha = await _context.Campanhas.FindAsync(id);

            if (campanha is null)
                throw new ArgumentNullException(nameof(campanha));

            campanha.Ativo = false;

            return campanha;
        }
    }
}
