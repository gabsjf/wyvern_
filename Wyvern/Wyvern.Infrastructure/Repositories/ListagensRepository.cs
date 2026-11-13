using Microsoft.EntityFrameworkCore;
using Wyvern.Domain.Consultas;
using Wyvern.Domain.Entities;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;
using PersonagemEntity = Wyvern.Domain.Entities.Personagem;
using SessaoEntity = Wyvern.Domain.Entities.Sessao;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories
{
    public class ListagensRepository : IListagensRepository
    {
        private const int Heroi = 1;
        private const int Npc = 2;
        private const int Monstro = 3;

        private const int CampanhasNoPainel = 6;
        private const int SessoesNoPainel = 5;
        private const int HeroisNoPainel = 8;

        private readonly WyvernDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ListagensRepository(WyvernDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        private int? UsuarioId => _currentUser.UserId;

        // ---------- campanhas ----------

        private IQueryable<CampanhaEntity> CampanhasVisiveis() =>
            _context.Campanhas.AsNoTracking().Where(Visibilidade.DaCampanha(UsuarioId));

        /// <summary>Projeta já com papel e números; a ordem e o filtro vêm antes, ainda sobre a entidade.</summary>
        private IQueryable<CampanhaNaLista> NaLista(IQueryable<CampanhaEntity> campanhas)
        {
            var usuarioId = UsuarioId;
            return campanhas.Select(c => new CampanhaNaLista(
                c.CampanhaId,
                c.Nome,
                c.Sistema,
                c.Ativo,
                c.Mestre != null ? c.Mestre.Nome : string.Empty,
                c.Membros!.Where(m => m.UsuarioId == usuarioId).Select(m => m.Papel).FirstOrDefault(),
                _context.Sessoes.Count(s => s.CampanhaId == c.CampanhaId && s.Ativo),
                _context.Personagens.Count(p => p.CampanhaId == c.CampanhaId && p.Ativo && p.TipoId == Heroi)));
        }

        public async Task<Pagina<CampanhaNaLista>> CampanhasAsync(PedidoDePagina pedido, string? busca)
        {
            var consulta = CampanhasVisiveis();
            var termo = Termo(busca);
            if (termo != null)
                consulta = consulta.Where(c => c.Nome.ToLower().Contains(termo) || (c.Sistema != null && c.Sistema.ToLower().Contains(termo)));

            var total = await consulta.CountAsync();
            var itens = await NaLista(consulta.OrderByDescending(c => c.CampanhaId).Skip(pedido.Pular).Take(pedido.Tamanho)).ToListAsync();
            return new Pagina<CampanhaNaLista>(itens, total, pedido.Numero, pedido.Tamanho);
        }

        // ---------- personagens ----------

        public async Task<PaginaDePersonagens> PersonagensAsync(PedidoDePagina pedido, string? busca, bool herois, int? campanhaId)
        {
            var visiveis = _context.Personagens.AsNoTracking().Where(Visibilidade.DoPersonagem(UsuarioId));

            var porTipo = await visiveis.GroupBy(p => p.TipoId == Heroi).Select(g => new { Heroi = g.Key, Total = g.Count() }).ToListAsync();
            var totalHerois = porTipo.FirstOrDefault(t => t.Heroi)?.Total ?? 0;
            var totalMonstros = porTipo.FirstOrDefault(t => !t.Heroi)?.Total ?? 0;

            var daAba = herois ? visiveis.Where(p => p.TipoId == Heroi) : visiveis.Where(p => p.TipoId != Heroi);
            var porCampanha = await ContarPorCampanha(daAba.Select(p => new ItemDeCampanha { CampanhaId = p.CampanhaId, Nome = p.Campanha.Nome }));

            var consulta = daAba;
            if (campanhaId != null)
                consulta = consulta.Where(p => p.CampanhaId == campanhaId);
            var termo = Termo(busca);
            if (termo != null)
                consulta = consulta.Where(p =>
                    p.Nome.ToLower().Contains(termo)
                    || (p.Descricao != null && p.Descricao.ToLower().Contains(termo))
                    || p.Campanha.Nome.ToLower().Contains(termo)
                    || (p.CriadoPor != null && p.CriadoPor.Nome.ToLower().Contains(termo)));

            var total = await consulta.CountAsync();
            var itens = await consulta
                .Include(p => p.Campanha)
                .Include(p => p.CriadoPor)
                .OrderBy(p => p.Campanha.Nome).ThenBy(p => p.Nome).ThenBy(p => p.PersonagemId)
                .Skip(pedido.Pular).Take(pedido.Tamanho)
                .ToListAsync();

            return new PaginaDePersonagens(new Pagina<PersonagemEntity>(itens, total, pedido.Numero, pedido.Tamanho), totalHerois, totalMonstros, porCampanha);
        }

        // ---------- sessões ----------

        private IQueryable<SessaoEntity> SessoesVisiveis() =>
            _context.Sessoes.AsNoTracking().Where(Visibilidade.DaSessao(UsuarioId));

        public async Task<PaginaDeSessoes> SessoesAsync(PedidoDePagina pedido, int? campanhaId)
        {
            var visiveis = SessoesVisiveis();
            var porCampanha = await ContarPorCampanha(visiveis.Select(s => new ItemDeCampanha { CampanhaId = s.CampanhaId, Nome = s.Campanha!.Nome }));

            var consulta = campanhaId != null ? visiveis.Where(s => s.CampanhaId == campanhaId) : visiveis;
            var total = await consulta.CountAsync();
            var itens = await consulta
                .Include(s => s.Campanha)
                .OrderBy(s => s.Campanha!.Nome).ThenBy(s => s.CampanhaId).ThenByDescending(s => s.NumeroSessao).ThenByDescending(s => s.SessaoId)
                .Skip(pedido.Pular).Take(pedido.Tamanho)
                .ToListAsync();

            return new PaginaDeSessoes(new Pagina<SessaoEntity>(itens, total, pedido.Numero, pedido.Tamanho), porCampanha);
        }

        private static IQueryable<SessaoNoPainel> NoPainel(IQueryable<SessaoEntity> sessoes) =>
            sessoes.Select(s => new SessaoNoPainel(s.SessaoId, s.NumeroSessao, s.Nome, s.DataSessao, s.DataAgendada, s.CampanhaId, s.Campanha!.Nome));

        // ---------- painel ----------

        public async Task<ResumoDoPainel> PainelAsync(DateTime hoje)
        {
            var campanhas = CampanhasVisiveis();
            var totalCampanhas = await campanhas.CountAsync();
            var recentes = await NaLista(campanhas.OrderByDescending(c => c.CampanhaId).Take(CampanhasNoPainel)).ToListAsync();
            var destaque = recentes.FirstOrDefault();

            var sessoes = SessoesVisiveis();
            var totalSessoes = await sessoes.CountAsync();
            var ultimaDoDestaque = destaque == null ? null : await NoPainel(sessoes
                .Where(s => s.CampanhaId == destaque.CampanhaId)
                .OrderByDescending(s => s.NumeroSessao).ThenByDescending(s => s.SessaoId)).FirstOrDefaultAsync();

            var proximas = await NoPainel(sessoes
                .Where(s => s.DataAgendada != null && s.DataAgendada >= hoje)
                .OrderBy(s => s.DataAgendada).ThenBy(s => s.SessaoId)
                .Take(SessoesNoPainel)).ToListAsync();
            var sessoesRecentes = await NoPainel(sessoes
                .OrderByDescending(s => s.DataSessao).ThenByDescending(s => s.SessaoId)
                .Take(SessoesNoPainel)).ToListAsync();

            var personagens = _context.Personagens.AsNoTracking().Where(Visibilidade.DoPersonagem(UsuarioId));
            var porTipo = await personagens.GroupBy(p => p.TipoId).Select(g => new { Tipo = g.Key, Total = g.Count() }).ToListAsync();
            int Total(int tipo) => porTipo.FirstOrDefault(t => t.Tipo == tipo)?.Total ?? 0;

            var herois = await personagens
                .Where(p => p.TipoId == Heroi)
                .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.PersonagemId)
                .Take(HeroisNoPainel)
                .Select(p => new HeroiNoPainel(p.PersonagemId, p.Nome, p.CampanhaId, p.Campanha.Nome))
                .ToListAsync();

            return new ResumoDoPainel(
                totalCampanhas, totalSessoes, Total(Heroi), Total(Npc), Total(Monstro),
                destaque, ultimaDoDestaque, recentes, proximas, sessoesRecentes, herois);
        }

        // ---------- apoio ----------

        private static string? Termo(string? busca)
        {
            var termo = busca?.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(termo) ? null : termo;
        }

        /// <summary>
        /// Conta por campanha e ordena no banco, com a mesma ordem (nome, depois id) das listas. Assim a tela sabe
        /// em que posição da lista começa cada campanha.
        /// </summary>
        private sealed class ItemDeCampanha
        {
            public int CampanhaId { get; set; }
            public string Nome { get; set; } = string.Empty;
        }

        private static async Task<IReadOnlyList<ContagemPorCampanha>> ContarPorCampanha(IQueryable<ItemDeCampanha> itens)
        {
            var contagens = await itens
                .GroupBy(i => new { i.CampanhaId, i.Nome })
                .Select(g => new { g.Key.CampanhaId, g.Key.Nome, Total = g.Count() })
                .OrderBy(c => c.Nome).ThenBy(c => c.CampanhaId)
                .ToListAsync();
            return contagens.Select(c => new ContagemPorCampanha(c.CampanhaId, c.Nome, c.Total)).ToList();
        }
    }
}
