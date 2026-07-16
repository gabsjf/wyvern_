using PersonagemEntity = Wyvern.Domain.Entities.Personagem;

namespace Wyvern.Infrastructure.Repositories.Personagem
{
    public interface IPersonagemRepository
    {
        Task<IEnumerable<PersonagemEntity>> GetPersonagensAsync();
        Task<PersonagemEntity?> GetPersonagemAsync(int id);
        Task<PersonagemEntity> CreatePersonagemAsync(PersonagemEntity personagem);
        Task<PersonagemEntity> UpdatePersonagemAsync(PersonagemEntity personagem);
        Task<PersonagemEntity> DeletePersonagemAsync(int id);
    }
}
