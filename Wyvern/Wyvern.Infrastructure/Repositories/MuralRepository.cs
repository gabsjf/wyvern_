using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories
{
    public class MuralRepository : IMuralRepository
    {
        private readonly WyvernDbContext _context;

        public MuralRepository(WyvernDbContext context) => _context = context;

        public Task<List<MuralResumo>> ListarAsync(int campanhaId) =>
            _context.Murais.AsNoTracking()
                .Where(m => m.CampanhaId == campanhaId)
                .OrderByDescending(m => m.AtualizadoEm).ThenByDescending(m => m.MuralId)
                .Select(m => new MuralResumo(m.MuralId, m.Nome, m.AtualizadoEm))
                .ToListAsync();

        public Task<Mural?> GetAsync(int campanhaId, int muralId) =>
            _context.Murais.FirstOrDefaultAsync(m => m.MuralId == muralId && m.CampanhaId == campanhaId);

        public async Task AddAsync(Mural mural) => await _context.Murais.AddAsync(mural);

        public void Remover(Mural mural) => _context.Murais.Remove(mural);

        public Task<MuralImagem?> GetImagemAsync(int muralId, int imagemId) =>
            _context.MuralImagens.AsNoTracking().FirstOrDefaultAsync(i => i.MuralImagemId == imagemId && i.MuralId == muralId);

        public async Task AddImagemAsync(MuralImagem imagem) => await _context.MuralImagens.AddAsync(imagem);

        public Task<int> ContarImagensAsync(int muralId) => _context.MuralImagens.CountAsync(i => i.MuralId == muralId);

        public async Task<int> RemoverImagensSemUsoAsync(int muralId, IReadOnlyCollection<int> emUso, DateTime anterioresA)
        {
            var sobrando = await _context.MuralImagens
                .Where(i => i.MuralId == muralId && i.CriadoEm < anterioresA && !emUso.Contains(i.MuralImagemId))
                .ToListAsync();
            _context.MuralImagens.RemoveRange(sobrando);
            return sobrando.Count;
        }
    }
}
