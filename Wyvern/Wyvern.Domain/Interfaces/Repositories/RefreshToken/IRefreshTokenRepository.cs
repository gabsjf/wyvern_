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

        /// <summary>Revoga todas as sessões do usuário e devolve as famílias que estavam ativas (para limpar o cache).</summary>
        Task<List<Guid>> RevogarTodasAsync(int usuarioId, DateTime agora);

        /// <summary>Apaga todos os tokens do usuário (exclusão de conta: o rótulo do aparelho também é dado pessoal).</summary>
        Task ApagarTodosAsync(int usuarioId);

        /// <summary>Tokens ainda válidos do usuário (para montar a lista de aparelhos conectados).</summary>
        Task<List<RefreshTokenEntity>> ListarAtivosAsync(int usuarioId, DateTime agora);

        /// <summary>A família pertence a este usuário (impede encerrar a sessão de outra pessoa).</summary>
        Task<bool> FamiliaDoUsuarioAsync(int usuarioId, Guid familiaId);

        /// <summary>Revoga as sessões do usuário, menos a atual, e devolve as famílias revogadas (para limpar o cache).</summary>
        Task<List<Guid>> RevogarOutrasAsync(int usuarioId, Guid familiaAtual, DateTime agora);

        /// <summary>A família existe, não foi revogada e o dono continua ativo.</summary>
        Task<bool> SessaoAtivaAsync(Guid familiaId);

        /// <summary>Apaga os tokens vencidos do usuário (limpeza oportunista a cada troca, sem job em segundo plano).</summary>
        Task ApagarExpiradosAsync(int usuarioId, DateTime agora);
    }
}
