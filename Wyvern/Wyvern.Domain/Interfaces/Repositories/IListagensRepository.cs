using System;
using System.Threading.Tasks;
using Wyvern.Domain.Consultas;

namespace Wyvern.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Listas paginadas e o painel inicial, sempre do ponto de vista do usuário atual.
    /// Busca, filtro, ordem e contagem acontecem no banco: só a página pedida sai de lá.
    /// </summary>
    public interface IListagensRepository
    {
        /// <summary>Campanhas, da mais nova para a mais antiga. A busca olha o nome e o sistema.</summary>
        Task<Pagina<CampanhaNaLista>> CampanhasAsync(PedidoDePagina pedido, string? busca);

        /// <summary>
        /// Heróis (<paramref name="herois"/> = true) ou NPCs e monstros, por campanha e nome. A busca olha nome, descrição,
        /// campanha e dono. As contagens das abas e dos filtros por campanha ignoram a busca e o filtro de campanha.
        /// </summary>
        Task<PaginaDePersonagens> PersonagensAsync(PedidoDePagina pedido, string? busca, bool herois, int? campanhaId);

        /// <summary>Sessões por campanha e, dentro dela, da mais nova para a mais antiga.</summary>
        Task<PaginaDeSessoes> SessoesAsync(PedidoDePagina pedido, int? campanhaId);

        /// <summary>O painel inicial. <paramref name="hoje"/> separa as sessões agendadas das que já passaram.</summary>
        Task<ResumoDoPainel> PainelAsync(DateTime hoje);
    }
}
