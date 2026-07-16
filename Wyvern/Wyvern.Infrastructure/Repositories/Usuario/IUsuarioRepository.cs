using UsuarioEntity = Wyvern.Domain.Entities.Usuario;

namespace Wyvern.Infrastructure.Repositories.Usuario
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<UsuarioEntity>> GetUsuariosAsync();
        Task<UsuarioEntity?> GetUsuarioAsync(int id);
        Task<UsuarioEntity?> GetByEmailAsync(string email);
        Task<UsuarioEntity> CreateUsuarioAsync(UsuarioEntity usuario);
        Task<UsuarioEntity> UpdateUsuarioAsync(UsuarioEntity usuario);
        Task<UsuarioEntity> DeleteUsuarioAsync(int id);
    }
}
