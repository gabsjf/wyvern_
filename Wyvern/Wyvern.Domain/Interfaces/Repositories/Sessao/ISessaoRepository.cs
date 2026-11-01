using SessaoEntity = Wyvern.Domain.Entities.Sessao;

namespace Wyvern.Domain.Interfaces.Repositories.Sessao
{
    public interface ISessaoRepository
    {
        Task<IEnumerable<SessaoEntity>> GetSessoesAsync();
        Task<SessaoEntity?> GetSessaoAsync(int id);
        /// <summary>Id da campanha de uma sessão ativa, sem filtrar pelo usuário atual; só para decisões de autorização.</summary>
        Task<int?> GetCampanhaIdAsync(int sessaoId);
        Task<SessaoEntity> CreateSessaoAsync(SessaoEntity sessao);
        Task<SessaoEntity> UpdateSessaoAsync(SessaoEntity sessao);
        Task<SessaoEntity> DeleteSessaoAsync(int id);
    }
}
