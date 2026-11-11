using System;
using System.Collections.Generic;
using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Consultas
{
    /// <summary>Um pedaço de uma lista: os itens da página e quantos existem ao todo.</summary>
    public record Pagina<T>(IReadOnlyList<T> Itens, int Total, int Numero, int Tamanho)
    {
        public int TotalPaginas => Tamanho <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamanho);
    }

    /// <summary>Pedido de página já saneado: número a partir de 1 e tamanho entre 1 e <see cref="TamanhoMaximo"/>.</summary>
    public readonly record struct PedidoDePagina
    {
        public const int TamanhoPadrao = 12;
        public const int TamanhoMaximo = 48;

        public int Numero { get; }
        public int Tamanho { get; }
        public int Pular => (Numero - 1) * Tamanho;

        public PedidoDePagina(int? numero, int? tamanho)
        {
            Numero = Math.Max(1, numero ?? 1);
            Tamanho = Math.Clamp(tamanho ?? TamanhoPadrao, 1, TamanhoMaximo);
        }
    }

    /// <summary>Quantos itens de uma lista são de cada campanha (os filtros da tela).</summary>
    public record ContagemPorCampanha(int CampanhaId, string Nome, int Total);

    /// <summary>Uma campanha na estante, já com os números do rodapé da capa.</summary>
    public record CampanhaNaLista(
        int CampanhaId, string Nome, string Sistema, bool Ativo, string MestreNome, PapelCampanha Papel,
        int TotalSessoes, int TotalHerois);

    public record PaginaDePersonagens(
        Pagina<Personagem> Pagina, int TotalHerois, int TotalMonstros, IReadOnlyList<ContagemPorCampanha> PorCampanha);

    public record PaginaDeSessoes(Pagina<Sessao> Pagina, IReadOnlyList<ContagemPorCampanha> PorCampanha);

    /// <summary>Uma sessão no painel, com o nome da campanha.</summary>
    public record SessaoNoPainel(int SessaoId, int NumeroSessao, string Nome, DateTime DataSessao, DateTime? DataAgendada, int CampanhaId, string CampanhaNome);

    /// <summary>Um herói no painel, com o nome da campanha.</summary>
    public record HeroiNoPainel(int PersonagemId, string Nome, int CampanhaId, string CampanhaNome);

    /// <summary>Tudo o que o painel inicial mostra, contado no banco.</summary>
    public record ResumoDoPainel(
        int TotalCampanhas, int TotalSessoes, int TotalHerois, int TotalNpcs, int TotalMonstros,
        CampanhaNaLista? Destaque, SessaoNoPainel? UltimaSessaoDoDestaque,
        IReadOnlyList<CampanhaNaLista> Campanhas,
        IReadOnlyList<SessaoNoPainel> ProximasSessoes, IReadOnlyList<SessaoNoPainel> SessoesRecentes,
        IReadOnlyList<HeroiNoPainel> Herois);
}
