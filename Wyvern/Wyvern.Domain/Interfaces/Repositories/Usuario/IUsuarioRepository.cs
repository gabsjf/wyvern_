using UsuarioEntity = Wyvern.Domain.Entities.Usuario;

namespace Wyvern.Domain.Interfaces.Repositories.Usuario
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<UsuarioEntity>> GetUsuariosAsync();
        Task<UsuarioEntity?> GetUsuarioAsync(int id);
        Task<UsuarioEntity?> GetByEmailAsync(string email);
        /// <summary>Só a linha do usuário ativo, sem campanhas e sem rastreio: para leituras frequentes como /Auth/me.</summary>
        Task<UsuarioEntity?> GetResumoAsync(int id);
        Task<UsuarioEntity> CreateUsuarioAsync(UsuarioEntity usuario);
        Task<UsuarioEntity> UpdateUsuarioAsync(UsuarioEntity usuario);

        /// <summary>Alguma outra conta, ativa ou não, já usa este e-mail (o índice único vale para todas).</summary>
        Task<bool> EmailEmUsoPorOutraContaAsync(string email, int usuarioId);

        Task<Wyvern.Domain.Entities.UsuarioFoto?> GetFotoAsync(int usuarioId);
        /// <summary>Cria a foto ou substitui a atual.</summary>
        Task SalvarFotoAsync(int usuarioId, string tipo, byte[] dados);
        Task RemoverFotoAsync(int usuarioId);

        /// <summary>
        /// Exclusão de conta: apaga as anotações privadas e as pastas privadas da pessoa, tira a autoria das públicas, solta
        /// os tokens que ela controlava e a remove de todas as campanhas. Só marca as mudanças; quem chama faz o commit.
        /// </summary>
        Task DesvincularContaAsync(int usuarioId);
    }
}
