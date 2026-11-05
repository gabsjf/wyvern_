using RefreshTokenEntity = Wyvern.Domain.Entities.RefreshToken;

namespace Wyvern.Domain.Interfaces.Repositories.RefreshToken
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshTokenEntity?> GetByHashAsync(string tokenHash);
        void Add(RefreshTokenEntity token);

        /// <summary>
        /// Marca o token como usado só se ainda não foi (UPDATE atômico). Falso = outra requisição o trocou antes,
        /// o que evita que duas trocas simultâneas bifurquem a família.
        /// </summary>
        Task<bool> MarcarComoUsadoAsync(int refreshTokenId, DateTime agora);

        Task RevogarFamiliaAsync(Guid familiaId, DateTime agora);

        /// <summary>Apaga os tokens vencidos do usuário (limpeza oportunista a cada troca, sem job em segundo plano).</summary>
        Task ApagarExpiradosAsync(int usuarioId, DateTime agora);
    }
}
