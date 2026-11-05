using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Domain.Interfaces.Repositories.Campanha;
using Wyvern.Domain.Interfaces.Repositories.Item;
using Wyvern.Domain.Interfaces.Repositories.Magia;
using Wyvern.Domain.Interfaces.Repositories.Pericia;
using Wyvern.Domain.Interfaces.Repositories.Personagem;
using Wyvern.Domain.Interfaces.Repositories.Sessao;
using Wyvern.Domain.Interfaces.Repositories.Usuario;
using Wyvern.Domain.Interfaces.Repositories.PastaAnotacao;
using Wyvern.Domain.Interfaces.Repositories.Cronica;

namespace Wyvern.Domain.Interfaces.Repositories
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
        Wyvern.Domain.Interfaces.Repositories.RefreshToken.IRefreshTokenRepository RefreshTokenRepository { get; }
        Wyvern.Domain.Interfaces.Repositories.Combate.ICombateRepository CombateRepository { get; }
        Wyvern.Domain.Interfaces.Repositories.Anotacao.IAnotacaoRepository AnotacaoRepository { get; }
        IPastaAnotacaoRepository PastaAnotacaoRepository { get; }
        IEventoCampanhaRepository EventoCampanhaRepository { get; }
        Wyvern.Domain.Interfaces.Repositories.Cena.ICenaRepository CenaRepository { get; }
        Task CommitAsync();

    }
}
