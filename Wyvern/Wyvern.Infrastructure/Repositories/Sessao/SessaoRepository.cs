using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using SessaoEntity = Wyvern.Domain.Entities.Sessao;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories.Sessao;

namespace Wyvern.Infrastructure.Repositories.Sessao
{
    public class SessaoRepository : ISessaoRepository
    {
        private readonly WyvernDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public SessaoRepository(WyvernDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // Só sessões de campanhas ativas das quais o usuário atual é mestre ou jogador.
        private IQueryable<SessaoEntity> SessoesVisiveis()
        {
            var userId = _currentUser.UserId;

            return _context.Sessoes
                .Include(s => s.Campanha)
                .Where(s => s.Ativo
                    && s.Campanha.Ativo
                    && (s.Campanha.MestreId == userId || s.Campanha.Jogadores!.Any(j => j.UsuarioId == userId)));
        }

        public async Task<IEnumerable<SessaoEntity>> GetSessoesAsync()
        {
            return await SessoesVisiveis().ToListAsync();
        }

        public async Task<SessaoEntity?> GetSessaoAsync(int id)
        {
            return await SessoesVisiveis().FirstOrDefaultAsync(s => s.SessaoId == id);
        }

        public async Task<int?> GetCampanhaIdAsync(int sessaoId)
        {
            return await _context.Sessoes
                .AsNoTracking()
                .Where(s => s.SessaoId == sessaoId && s.Ativo)
                .Select(s => (int?)s.CampanhaId)
                .FirstOrDefaultAsync();
        }

        public Task<SessaoEntity> CreateSessaoAsync(SessaoEntity sessao)
        {
            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            _context.Sessoes.Add(sessao);

            return Task.FromResult(sessao);
        }

        public Task<SessaoEntity> UpdateSessaoAsync(SessaoEntity sessao)
        {
            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            _context.Entry(sessao).State = EntityState.Modified;

            return Task.FromResult(sessao);
        }

        public async Task<SessaoEntity> DeleteSessaoAsync(int id)
        {
            var sessao = await _context.Sessoes.FindAsync(id);

            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            sessao.Ativo = false;

            return sessao;
        }
    }
}
