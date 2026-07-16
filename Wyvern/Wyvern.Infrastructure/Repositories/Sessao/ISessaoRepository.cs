using SessaoEntity = Wyvern.Domain.Entities.Sessao;

namespace Wyvern.Infrastructure.Repositories.Sessao
{
    public interface ISessaoRepository
    {
        Task<IEnumerable<SessaoEntity>> GetSessoesAsync();
        Task<SessaoEntity?> GetSessaoAsync(int id);
        Task<SessaoEntity> CreateSessaoAsync(SessaoEntity sessao);
        Task<SessaoEntity> UpdateSessaoAsync(SessaoEntity sessao);
        Task<SessaoEntity> DeleteSessaoAsync(int id);
    }
}
