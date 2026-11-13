using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories
{
    public class Modelo3dRepository : IModelo3dRepository
    {
        private readonly WyvernDbContext _context;

        public Modelo3dRepository(WyvernDbContext context) => _context = context;

        public Task<List<Modelo3d>> ListarAsync(int campanhaId) =>
            _context.Modelos3d.AsNoTracking()
                .Include(m => m.EnviadoPor)
                .Where(m => m.CampanhaId == campanhaId)
                .OrderBy(m => m.Categoria).ThenBy(m => m.Nome).ThenBy(m => m.ModeloId)
                .ToListAsync();

        public Task<Modelo3d?> GetAsync(int campanhaId, int modeloId) =>
            _context.Modelos3d.Include(m => m.EnviadoPor).FirstOrDefaultAsync(m => m.ModeloId == modeloId && m.CampanhaId == campanhaId);

        public Task<byte[]?> GetDadosAsync(int modeloId) =>
            _context.Modelos3dArquivos.AsNoTracking().Where(a => a.ModeloId == modeloId).Select(a => (byte[]?)a.Dados).FirstOrDefaultAsync();

        public async Task<long> EspacoUsadoAsync(int campanhaId) =>
            await _context.Modelos3d.Where(m => m.CampanhaId == campanhaId).SumAsync(m => (long?)m.Tamanho) ?? 0;

        public async Task AddAsync(Modelo3d modelo, byte[] dados)
        {
            await _context.Modelos3d.AddAsync(modelo);
            await _context.Modelos3dArquivos.AddAsync(new Modelo3dArquivo { Modelo = modelo, Dados = dados });
        }

        public void Remover(Modelo3d modelo) => _context.Modelos3d.Remove(modelo);

        public async Task SubstituirAsync(Modelo3d antigo, Modelo3d novo, byte[] dados)
        {
            await AddAsync(novo, dados);
            // pela navegação: o id do novo só existe ao gravar, e o EF grava o novo antes de atualizar quem aponta para ele
            foreach (var o in await _context.ObjetosCena.Where(o => o.ModeloId == antigo.ModeloId).ToListAsync()) o.Modelo = novo;
            foreach (var t in await _context.TokensCena.Where(t => t.ModeloId == antigo.ModeloId).ToListAsync()) t.Modelo = novo;
            foreach (var p in await _context.Personagens.Where(p => p.ModeloId == antigo.ModeloId).ToListAsync()) p.Modelo = novo;
            _context.Modelos3d.Remove(antigo);
        }

        public Task<List<ObjetoCena>> ListarObjetosAsync(int cenaId) =>
            _context.ObjetosCena.AsNoTracking().Include(o => o.Modelo)
                .Where(o => o.CenaId == cenaId).OrderBy(o => o.ObjetoId).ToListAsync();

        public Task<ObjetoCena?> GetObjetoAsync(int cenaId, int objetoId) =>
            _context.ObjetosCena.Include(o => o.Modelo).FirstOrDefaultAsync(o => o.CenaId == cenaId && o.ObjetoId == objetoId);

        public Task<int> ContarObjetosAsync(int cenaId) => _context.ObjetosCena.CountAsync(o => o.CenaId == cenaId);

        public async Task AddObjetoAsync(ObjetoCena objeto) => await _context.ObjetosCena.AddAsync(objeto);

        public void RemoverObjeto(ObjetoCena objeto) => _context.ObjetosCena.Remove(objeto);

        public Task<List<int>> CenasQueUsamAsync(int modeloId) =>
            _context.ObjetosCena.Where(o => o.ModeloId == modeloId).Select(o => o.CenaId)
                .Union(_context.TokensCena.Where(t => t.ModeloId == modeloId || t.Personagem!.ModeloId == modeloId).Select(t => t.CenaId))
                .Distinct().ToListAsync();
    }
}
