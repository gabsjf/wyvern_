using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using SessaoEntity = Wyvern.Domain.Entities.Sessao;

namespace Wyvern.Infrastructure.Repositories.Sessao
{
    public class SessaoRepository : ISessaoRepository
    {
        private readonly WyvernDbContext _context;

        public SessaoRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SessaoEntity>> GetSessoesAsync()
        {
            return await _context.Sessoes
                .Include(s => s.Campanha)
                .Where(s => s.Ativo)
                .ToListAsync();
        }

        public async Task<SessaoEntity?> GetSessaoAsync(int id)
        {
            return await _context.Sessoes
                .Include(s => s.Campanha)
                .FirstOrDefaultAsync(s => s.SessaoId == id && s.Ativo);
        }

        public async Task<SessaoEntity> CreateSessaoAsync(SessaoEntity sessao)
        {
            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            _context.Sessoes.Add(sessao);
            await _context.SaveChangesAsync();

            return sessao;
        }

        public async Task<SessaoEntity> UpdateSessaoAsync(SessaoEntity sessao)
        {
            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            _context.Entry(sessao).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return sessao;
        }

        public async Task<SessaoEntity> DeleteSessaoAsync(int id)
        {
            var sessao = await _context.Sessoes.FindAsync(id);

            if (sessao is null)
                throw new ArgumentNullException(nameof(sessao));

            sessao.Ativo = false;
            await _context.SaveChangesAsync();

            return sessao;
        }
    }
}
