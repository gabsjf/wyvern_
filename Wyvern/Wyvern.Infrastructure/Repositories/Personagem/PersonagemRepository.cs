using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using Wyvern.Domain.Interfaces;
using PersonagemEntity = Wyvern.Domain.Entities.Personagem;

namespace Wyvern.Infrastructure.Repositories.Personagem
{
    public class PersonagemRepository : IPersonagemRepository
    {
        private readonly WyvernDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public PersonagemRepository(WyvernDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<PersonagemEntity>> GetPersonagensAsync()
        {
            return await _context.Personagens
                .AsNoTracking()
                .Include(p => p.Atributo)
                .Include(p => p.PersonagemPlayer)
                .Include(p => p.PersonagemCombate)
                .Include(p => p.Campanha)
                .Where(p => p.Ativo && (p.CriadoPorId == _currentUser.UserId || p.Campanha.MestreId == _currentUser.UserId))
                .ToListAsync();
        }

        public async Task<PersonagemEntity?> GetPersonagemAsync(int id)
        {
            return await _context.Personagens
                .Include(p => p.PersonagemPlayer)
                .Include(p => p.Atributo)
                .Include(p => p.PersonagemCombate)
                .Include(p => p.PersonagemDetalhes)
                .Include(p => p.PersonagemDinheiro)
                .Include(p => p.PersonagemItens)
                .Include(p => p.PersonagemMagias)
                .Include(p => p.PersonagemAtaques)
                .Include(p => p.PersonagemNpc)
                .Include(p => p.PersonagemAcoesPadrao)
                .Include(p => p.PersonagemAcoesBonus)
                .Include(p => p.PersonagemReacoes)
                .Include(p => p.PersonagemAcoesLendarias)
                .Include(p => p.PersonagemTracosEspeciais)
                .Include(p => p.PersonagemConjuracao)
                .Include(p => p.PersonagemPericias!)
                    .ThenInclude(pp => pp.Pericia)
                .AsSplitQuery()
                .Include(p => p.Campanha)
                .FirstOrDefaultAsync(p => p.PersonagemId == id && p.Ativo && (p.CriadoPorId == _currentUser.UserId || p.Campanha.MestreId == _currentUser.UserId));
        }

        public async Task<PersonagemEntity> CreatePersonagemAsync(PersonagemEntity personagem)
        {
            if (personagem is null)
                throw new ArgumentNullException(nameof(personagem));

            if (_currentUser.UserId.HasValue)
            {
                personagem.CriadoPorId = _currentUser.UserId.Value;
            }

            _context.Personagens.Add(personagem);
            await _context.SaveChangesAsync();

            return personagem;
        }

        public async Task<PersonagemEntity> UpdatePersonagemAsync(PersonagemEntity personagem)
        {
            if (personagem is null)
                throw new ArgumentNullException(nameof(personagem));

            _context.Entry(personagem).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return personagem;
        }

        public async Task<PersonagemEntity> DeletePersonagemAsync(int id)
        {
            var personagem = await _context.Personagens.FindAsync(id);

            if (personagem is null)
                throw new ArgumentNullException(nameof(personagem));

            personagem.Ativo = false;
            await _context.SaveChangesAsync();

            return personagem;
        }
    }
}
