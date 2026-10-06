using System.Threading.Tasks;
using Wyvern.Infrastructure.Data;
using Wyvern.Infrastructure.Repositories.Campanha;
using Wyvern.Infrastructure.Repositories.Item;
using Wyvern.Infrastructure.Repositories.Magia;
using Wyvern.Infrastructure.Repositories.Pericia;
using Wyvern.Infrastructure.Repositories.Personagem;
using Wyvern.Infrastructure.Repositories.Sessao;
using Wyvern.Infrastructure.Repositories.Usuario;
using Wyvern.Infrastructure.Repositories.Combate;
using Wyvern.Infrastructure.Repositories.Anotacao;
using Wyvern.Infrastructure.Repositories.PastaAnotacao;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Wyvern.Domain.Interfaces.Repositories.Campanha;
using Wyvern.Domain.Interfaces.Repositories.Item;
using Wyvern.Domain.Interfaces.Repositories.Magia;
using Wyvern.Domain.Interfaces.Repositories.Pericia;
using Wyvern.Domain.Interfaces.Repositories.Personagem;
using Wyvern.Domain.Interfaces.Repositories.Sessao;
using Wyvern.Domain.Interfaces.Repositories.Usuario;
using Wyvern.Domain.Interfaces.Repositories.Combate;
using Wyvern.Domain.Interfaces.Repositories.Anotacao;
using Wyvern.Domain.Interfaces.Repositories.PastaAnotacao;

namespace Wyvern.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WyvernDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UnitOfWork(WyvernDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
            CampanhaRepository = new CampanhaRepository(_context, _currentUserService);
            ItemRepository = new ItemRepository(_context);
            MagiaRepository = new MagiaRepository(_context);
            PericiaRepository = new PericiaRepository(_context);
            PersonagemRepository = new PersonagemRepository(_context, _currentUserService);
            SessaoRepository = new SessaoRepository(_context);
            UsuarioRepository = new UsuarioRepository(_context);
            AnotacaoRepository = new AnotacaoRepository(_context);
            PastaAnotacaoRepository = new PastaAnotacaoRepository(_context);
            CombateRepository = new CombateRepository(_context);
        }

        public ICampanhaRepository CampanhaRepository { get; }
        public IItemRepository ItemRepository { get; }
        public IMagiaRepository MagiaRepository { get; }
        public IPericiaRepository PericiaRepository { get; }
        public IPersonagemRepository PersonagemRepository { get; }
        public ISessaoRepository SessaoRepository { get; }
        public IUsuarioRepository UsuarioRepository { get; }
        public IAnotacaoRepository AnotacaoRepository { get; }
        public IPastaAnotacaoRepository PastaAnotacaoRepository { get; }
        public ICombateRepository CombateRepository { get; }

        public void Commit()
        {
            _context.SaveChanges();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
