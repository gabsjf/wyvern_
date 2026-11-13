using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Regras;
using Wyvern.Infrastructure.Data;
using UsuarioEntity = Wyvern.Domain.Entities.Usuario;
using Wyvern.Domain.Interfaces.Repositories.Usuario;

namespace Wyvern.Infrastructure.Repositories.Usuario
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly WyvernDbContext _context;

        public UsuarioRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UsuarioEntity>> GetUsuariosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Campanhas)
                .Where(u => u.Ativo)
                .ToListAsync();
        }

        public async Task<UsuarioEntity?> GetUsuarioAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Campanhas)
                .FirstOrDefaultAsync(u => u.UsuarioId == id && u.Ativo);
        }

        public async Task<UsuarioEntity?> GetByEmailAsync(string email)
        {
            // Os e-mails são guardados em minúsculas (EmailDaConta): a busca normaliza do mesmo jeito.
            var normalizado = EmailDaConta.Normalizar(email);
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == normalizado && u.Ativo);
        }

        public Task<UsuarioEntity> CreateUsuarioAsync(UsuarioEntity usuario)
        {
            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            _context.Usuarios.Add(usuario);

            return Task.FromResult(usuario);
        }

        public Task<UsuarioEntity> UpdateUsuarioAsync(UsuarioEntity usuario)
        {
            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            _context.Entry(usuario).State = EntityState.Modified;

            return Task.FromResult(usuario);
        }

        public Task<UsuarioEntity?> GetResumoAsync(int id) =>
            _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioId == id && u.Ativo);

        public Task<bool> EmailEmUsoPorOutraContaAsync(string email, int usuarioId)
        {
            var normalizado = EmailDaConta.Normalizar(email);
            return _context.Usuarios.AnyAsync(u => u.Email == normalizado && u.UsuarioId != usuarioId);
        }

        public Task<UsuarioFoto?> GetFotoAsync(int usuarioId) =>
            _context.UsuarioFotos.AsNoTracking().FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);

        public async Task SalvarFotoAsync(int usuarioId, string tipo, byte[] dados)
        {
            var foto = await _context.UsuarioFotos.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);
            if (foto == null)
                _context.UsuarioFotos.Add(new UsuarioFoto { UsuarioId = usuarioId, Tipo = tipo, Dados = dados });
            else
            {
                foto.Tipo = tipo;
                foto.Dados = dados;
            }
        }

        public async Task RemoverFotoAsync(int usuarioId)
        {
            var foto = await _context.UsuarioFotos.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);
            if (foto != null) _context.UsuarioFotos.Remove(foto);
        }

        public async Task DesvincularContaAsync(int usuarioId)
        {
            // Anotações: as privadas eram só dela e somem; as públicas são do grupo e ficam, sem autor.
            var anotacoes = await _context.Anotacoes.Where(a => a.CriadoPorId == usuarioId).ToListAsync();
            _context.Anotacoes.RemoveRange(anotacoes.Where(a => !a.IsPublica));
            foreach (var publica in anotacoes.Where(a => a.IsPublica))
                publica.CriadoPorId = null;

            // Pastas: mesma regra. Uma pasta privada pode guardar anotação pública de outra pessoa: ela sai da pasta e fica.
            var pastas = await _context.PastasAnotacao.Where(p => p.CriadoPorId == usuarioId).ToListAsync();
            var privadas = pastas.Where(p => !p.IsPublica).Select(p => p.PastaId).ToList();
            if (privadas.Count > 0)
            {
                var restantes = await _context.Anotacoes.Where(a => a.PastaId != null && privadas.Contains(a.PastaId.Value)).ToListAsync();
                foreach (var anotacao in restantes)
                    anotacao.PastaId = null;
            }
            _context.PastasAnotacao.RemoveRange(pastas.Where(p => !p.IsPublica));
            foreach (var publica in pastas.Where(p => p.IsPublica))
                publica.CriadoPorId = null;

            // Tokens do mapa que ela movia ficam com o Mestre.
            foreach (var token in await _context.TokensCena.Where(t => t.ControladoPorId == usuarioId).ToListAsync())
                token.ControladoPorId = null;

            _context.CampanhasMembros.RemoveRange(await _context.CampanhasMembros.Where(m => m.UsuarioId == usuarioId).ToListAsync());
            await RemoverFotoAsync(usuarioId);
        }
    }
}
