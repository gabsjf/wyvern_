using PericiaEntity = Wyvern.Domain.Entities.Pericia;

namespace Wyvern.Infrastructure.Repositories.Pericia
{
    public interface IPericiaRepository
    {
        Task<IEnumerable<PericiaEntity>> GetPericiasAsync();
        Task<PericiaEntity?> GetPericiaAsync(int id);
        Task<PericiaEntity> CreatePericiaAsync(PericiaEntity pericia);
        Task<PericiaEntity> UpdatePericiaAsync(PericiaEntity pericia);
        Task<PericiaEntity> DeletePericiaAsync(int id);
    }
}
