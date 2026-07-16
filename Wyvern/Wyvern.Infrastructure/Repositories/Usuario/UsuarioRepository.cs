using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using UsuarioEntity = Wyvern.Domain.Entities.Usuario;

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
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email && u.Ativo);
        }

        public async Task<UsuarioEntity> CreateUsuarioAsync(UsuarioEntity usuario)
        {
            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

        public async Task<UsuarioEntity> UpdateUsuarioAsync(UsuarioEntity usuario)
        {
            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return usuario;
        }

        public async Task<UsuarioEntity> DeleteUsuarioAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            usuario.Ativo = false;
            await _context.SaveChangesAsync();

            return usuario;
        }
    }
}
