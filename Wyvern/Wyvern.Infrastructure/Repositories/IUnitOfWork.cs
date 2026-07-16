using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Infrastructure.Repositories.Campanha;
using Wyvern.Infrastructure.Repositories.Item;
using Wyvern.Infrastructure.Repositories.Magia;
using Wyvern.Infrastructure.Repositories.Pericia;
using Wyvern.Infrastructure.Repositories.Personagem;
using Wyvern.Infrastructure.Repositories.Sessao;
using Wyvern.Infrastructure.Repositories.Usuario;
using Wyvern.Infrastructure.Repositories.PastaAnotacao;

namespace Wyvern.Infrastructure.Repositories
{
    public interface IUnitOfWork
    {
        ICampanhaRepository CampanhaRepository { get; }
        IItemRepository ItemRepository { get; }
        IMagiaRepository MagiaRepository { get; }
        IPericiaRepository PericiaRepository { get; }
        IPersonagemRepository PersonagemRepository { get; }
        ISessaoRepository SessaoRepository { get; }
        IUsuarioRepository UsuarioRepository { get; }
        Wyvern.Infrastructure.Repositories.Combate.ICombateRepository CombateRepository { get; }
        Wyvern.Infrastructure.Repositories.Anotacao.IAnotacaoRepository AnotacaoRepository { get; }
        IPastaAnotacaoRepository PastaAnotacaoRepository { get; }
        Task CommitAsync();

    }
}
