using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories.Cena;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories.Cena
{
    public class CenaRepository : ICenaRepository
    {
        private readonly WyvernDbContext _context;

        public CenaRepository(WyvernDbContext context) => _context = context;

        public Task<List<Domain.Entities.Cena>> GetPorCampanhaAsync(int campanhaId) =>
            _context.Cenas.AsNoTracking()
                .Where(c => c.CampanhaId == campanhaId)
                .OrderBy(c => c.Ordem).ThenBy(c => c.CenaId)
                .ToListAsync();

        public Task<Domain.Entities.Cena?> GetAsync(int campanhaId, int cenaId) =>
            _context.Cenas.FirstOrDefaultAsync(c => c.CampanhaId == campanhaId && c.CenaId == cenaId);

        public Task<Domain.Entities.Cena?> GetAtivaAsync(int campanhaId) =>
            _context.Cenas.AsNoTracking().FirstOrDefaultAsync(c => c.CampanhaId == campanhaId && c.Ativa);

        public async Task<int> MaiorOrdemAsync(int campanhaId) =>
            await _context.Cenas.Where(c => c.CampanhaId == campanhaId).Select(c => (int?)c.Ordem).MaxAsync() ?? 0;

        public Task<CenaImagem?> GetImagemAsync(int cenaId) =>
            _context.CenaImagens.AsNoTracking().FirstOrDefaultAsync(i => i.CenaId == cenaId);

        public Task<CenaImagem?> GetImagemParaEditarAsync(int cenaId) =>
            _context.CenaImagens.FirstOrDefaultAsync(i => i.CenaId == cenaId);

        public Task<List<TokenCena>> GetTokensAsync(int cenaId) =>
            _context.TokensCena.AsNoTracking()
                .Include(t => t.Personagem).ThenInclude(p => p!.PersonagemCombate)
                .Where(t => t.CenaId == cenaId)
                .OrderBy(t => t.TokenId)
                .ToListAsync();

        public Task<TokenCena?> GetTokenAsync(int cenaId, int tokenId) =>
            _context.TokensCena
                .Include(t => t.Cena).Include(t => t.Personagem).ThenInclude(p => p!.PersonagemCombate)
                .FirstOrDefaultAsync(t => t.CenaId == cenaId && t.TokenId == tokenId);

        public Task<TokenCena?> GetTokenDaCampanhaAsync(int campanhaId, int tokenId) =>
            _context.TokensCena
                .Include(t => t.Cena).Include(t => t.Personagem).ThenInclude(p => p!.PersonagemCombate)
                .FirstOrDefaultAsync(t => t.TokenId == tokenId && t.Cena!.CampanhaId == campanhaId);

        public Task<bool> PersonagemEhDaCampanhaAsync(int campanhaId, int personagemId) =>
            _context.Personagens.AnyAsync(p => p.PersonagemId == personagemId && p.CampanhaId == campanhaId);

        public Task<bool> ParticipanteEhDaCampanhaAsync(int campanhaId, int participanteId) =>
            _context.CombateParticipantes.AnyAsync(p => p.ParticipanteId == participanteId && p.Combate!.Sessao!.CampanhaId == campanhaId);

        public Task<TokenImagem?> GetTokenImagemAsync(int tokenId) =>
            _context.TokenImagens.AsNoTracking().FirstOrDefaultAsync(i => i.TokenId == tokenId);

        public Task<TokenImagem?> GetTokenImagemParaEditarAsync(int tokenId) =>
            _context.TokenImagens.FirstOrDefaultAsync(i => i.TokenId == tokenId);

        public Task AddTokenImagemAsync(TokenImagem imagem)
        {
            _context.TokenImagens.Add(imagem);
            return Task.CompletedTask;
        }

        public Task RemoveTokenImagemAsync(TokenImagem imagem)
        {
            _context.TokenImagens.Remove(imagem);
            return Task.CompletedTask;
        }

        public Task<List<ParedeCena>> GetParedesAsync(int cenaId) =>
            _context.ParedesCena.AsNoTracking().Where(p => p.CenaId == cenaId).OrderBy(p => p.ParedeId).ToListAsync();

        public Task<ParedeCena?> GetParedeAsync(int cenaId, int paredeId) =>
            _context.ParedesCena.FirstOrDefaultAsync(p => p.CenaId == cenaId && p.ParedeId == paredeId);

        public Task<int> ContarParedesAsync(int cenaId) => _context.ParedesCena.CountAsync(p => p.CenaId == cenaId);

        public Task AddParedeAsync(ParedeCena parede)
        {
            _context.ParedesCena.Add(parede);
            return Task.CompletedTask;
        }

        public Task AddParedesAsync(IEnumerable<ParedeCena> paredes)
        {
            _context.ParedesCena.AddRange(paredes);
            return Task.CompletedTask;
        }

        public Task RemoveParedeAsync(ParedeCena parede)
        {
            _context.ParedesCena.Remove(parede);
            return Task.CompletedTask;
        }

        public Task<List<MarcadorCena>> GetMarcadoresAsync(int cenaId) =>
            _context.MarcadoresCena.AsNoTracking().Where(m => m.CenaId == cenaId).OrderBy(m => m.MarcadorId).ToListAsync();

        public Task<MarcadorCena?> GetMarcadorAsync(int cenaId, int marcadorId) =>
            _context.MarcadoresCena.FirstOrDefaultAsync(m => m.CenaId == cenaId && m.MarcadorId == marcadorId);

        public Task AddMarcadorAsync(MarcadorCena marcador)
        {
            _context.MarcadoresCena.Add(marcador);
            return Task.CompletedTask;
        }

        public Task AddMarcadoresAsync(IEnumerable<MarcadorCena> marcadores)
        {
            _context.MarcadoresCena.AddRange(marcadores);
            return Task.CompletedTask;
        }

        public Task RemoveMarcadorAsync(MarcadorCena marcador)
        {
            _context.MarcadoresCena.Remove(marcador);
            return Task.CompletedTask;
        }

        public Task<List<TracoCena>> GetTracosAsync(int cenaId) =>
            _context.TracosCena.AsNoTracking().Where(t => t.CenaId == cenaId).OrderBy(t => t.TracoId).ToListAsync();

        public Task<TracoCena?> GetTracoAsync(int cenaId, int tracoId) =>
            _context.TracosCena.FirstOrDefaultAsync(t => t.CenaId == cenaId && t.TracoId == tracoId);

        public Task<int> ContarTracosAsync(int cenaId) => _context.TracosCena.CountAsync(t => t.CenaId == cenaId);

        public Task AddTracoAsync(TracoCena traco)
        {
            _context.TracosCena.Add(traco);
            return Task.CompletedTask;
        }

        public Task RemoveTracoAsync(TracoCena traco)
        {
            _context.TracosCena.Remove(traco);
            return Task.CompletedTask;
        }

        public async Task RemoveTodosOsTracosAsync(int cenaId)
        {
            _context.TracosCena.RemoveRange(await _context.TracosCena.Where(t => t.CenaId == cenaId).ToListAsync());
        }

        public Task AddAsync(Domain.Entities.Cena cena, CenaImagem imagem)
        {
            imagem.Cena = cena;
            _context.Cenas.Add(cena);
            _context.CenaImagens.Add(imagem);
            return Task.CompletedTask;
        }

        public Task AddTokensAsync(IEnumerable<TokenCena> tokens)
        {
            _context.TokensCena.AddRange(tokens);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Domain.Entities.Cena cena)
        {
            _context.Cenas.Remove(cena);
            return Task.CompletedTask;
        }

        public Task RemoveTokenAsync(TokenCena token)
        {
            _context.TokensCena.Remove(token);
            return Task.CompletedTask;
        }
    }
}
