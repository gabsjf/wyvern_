using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Interfaces.Repositories.RefreshToken;
using Wyvern.Infrastructure.Data;
using RefreshTokenEntity = Wyvern.Domain.Entities.RefreshToken;

namespace Wyvern.Infrastructure.Repositories.RefreshToken
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly WyvernDbContext _context;

        public RefreshTokenRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public Task<RefreshTokenEntity?> GetByHashAsync(string tokenHash) =>
            _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public void Add(RefreshTokenEntity token) => _context.RefreshTokens.Add(token);

        public async Task<bool> MarcarComoUsadoAsync(int refreshTokenId, DateTime agora) =>
            await _context.RefreshTokens
                .Where(t => t.RefreshTokenId == refreshTokenId && t.UsadoEm == null && t.RevogadoEm == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEm, agora)) == 1;

        public Task RevogarFamiliaAsync(Guid familiaId, DateTime agora) =>
            _context.RefreshTokens
                .Where(t => t.FamiliaId == familiaId && t.RevogadoEm == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevogadoEm, agora));

        public async Task<List<Guid>> RevogarTodasAsync(int usuarioId, DateTime agora)
        {
            var ativas = _context.RefreshTokens.Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null);
            var familias = await ativas.Select(t => t.FamiliaId).Distinct().ToListAsync();
            await ativas.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevogadoEm, agora));
            return familias;
        }

        public Task<bool> SessaoAtivaAsync(Guid familiaId) =>
            _context.RefreshTokens.AnyAsync(t => t.FamiliaId == familiaId && t.RevogadoEm == null && t.Usuario!.Ativo);

        public Task ApagarExpiradosAsync(int usuarioId, DateTime agora) =>
            _context.RefreshTokens
                .Where(t => t.UsuarioId == usuarioId && t.ExpiraEm < agora)
                .ExecuteDeleteAsync();
    }
}
