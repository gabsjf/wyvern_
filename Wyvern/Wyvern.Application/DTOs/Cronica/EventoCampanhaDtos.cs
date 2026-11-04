using System.ComponentModel.DataAnnotations;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Cronica
{
    /// <summary>O que o front envia para criar ou editar um evento.</summary>
    public class EventoCampanhaFormDto
    {
        [Required, StringLength(120, MinimumLength = 1)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Descricao { get; set; }

        [Required]
        public string Tipo { get; set; } = "Outro";

        /// <summary>Sessão da mesa em que aconteceu; precisa ser da mesma campanha.</summary>
        public int? SessaoId { get; set; }

        [StringLength(60)]
        public string? RotuloData { get; set; }

        /// <summary>Posição na cronologia do mundo. Sem valor, o evento vai para o fim.</summary>
        public int? Ordem { get; set; }

        /// <summary>Só o Mestre pode esconder um evento dos jogadores; para os demais isto é ignorado.</summary>
        public bool SoMestre { get; set; }
    }

    public class EventoCampanhaDto
    {
        public int EventoId { get; set; }
        public int CampanhaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? SessaoId { get; set; }
        public int? SessaoNumero { get; set; }
        public string? RotuloData { get; set; }
        public int Ordem { get; set; }
        /// <summary>"Todos" ou "Mestre".</summary>
        public string Visibilidade { get; set; } = "Todos";
        /// <summary>"Manual" ou "Automatico".</summary>
        public string Origem { get; set; } = "Manual";
        public int? AutorId { get; set; }
        public string AutorNome { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }

        public static EventoCampanhaDto De(EventoCampanha e) => new()
        {
            EventoId = e.EventoId,
            CampanhaId = e.CampanhaId,
            Titulo = e.Titulo,
            Descricao = e.Descricao,
            Tipo = e.Tipo,
            SessaoId = e.SessaoId,
            SessaoNumero = e.Sessao?.NumeroSessao,
            RotuloData = e.RotuloData,
            Ordem = e.Ordem,
            Visibilidade = e.SoMestre ? "Mestre" : "Todos",
            Origem = e.Automatico ? "Automatico" : "Manual",
            AutorId = e.CriadoPorId,
            AutorNome = e.Automatico ? "Sistema" : e.CriadoPor?.Nome ?? "Alguém que saiu",
            CriadoEm = e.CriadoEm,
        };
    }
}
