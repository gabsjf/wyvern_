using Microsoft.Extensions.Caching.Memory;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Confere no servidor se a sessão por trás de um access token ainda vale. O JWT sozinho não pode ser revogado; por isso
    /// ele leva a claim "sid" (a família do refresh token) e cada requisição checa que a família não foi revogada (logout,
    /// reuso detectado) e que o usuário continua ativo.
    /// </summary>
    public interface ISessaoValidator
    {
        Task<bool> EstaAtivaAsync(Guid sessaoId);

        /// <summary>Esquece o "ativa" em cache; chamado ao revogar, para o logout valer na hora e não só quando o cache vencer.</summary>
        void Invalidar(Guid sessaoId);
    }

    public class SessaoValidator : ISessaoValidator
    {
        // Só o "ativa" é guardado: uma sessão revogada é consultada no banco a cada vez, o que é raro e barato. A janela
        // abaixo vale só para revogações vindas de outra instância da API ou de uma edição direta no banco.
        private static readonly TimeSpan ValidadeDoCache = TimeSpan.FromSeconds(15);

        private readonly IUnitOfWork _uof;
        private readonly IMemoryCache _cache;

        public SessaoValidator(IUnitOfWork uof, IMemoryCache cache)
        {
            _uof = uof;
            _cache = cache;
        }

        public async Task<bool> EstaAtivaAsync(Guid sessaoId)
        {
            if (_cache.TryGetValue(Chave(sessaoId), out _))
                return true;

            var ativa = await _uof.RefreshTokenRepository.SessaoAtivaAsync(sessaoId);
            if (ativa)
                _cache.Set(Chave(sessaoId), true, ValidadeDoCache);
            return ativa;
        }

        public void Invalidar(Guid sessaoId) => _cache.Remove(Chave(sessaoId));

        private static string Chave(Guid sessaoId) => $"sessao-ativa:{sessaoId}";
    }
}
