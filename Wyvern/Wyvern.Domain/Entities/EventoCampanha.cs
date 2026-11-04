using System.ComponentModel.DataAnnotations;

namespace Wyvern.Domain.Entities
{
    /// <summary>Um acontecimento na crônica da campanha: escrito pelos membros ou registrado sozinho pelo sistema.</summary>
    public class EventoCampanha
    {
        [Key]
        public int EventoId { get; set; }

        public int CampanhaId { get; set; }
        public Campanha? Campanha { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Markdown.</summary>
        [StringLength(4000)]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Marco, Combate, Descoberta, Morte, Viagem, Diplomacia, Descanso ou Outro.</summary>
        [Required, StringLength(20)]
        public string Tipo { get; set; } = "Outro";

        /// <summary>Sessão da mesa em que aconteceu (visão "pela mesa").</summary>
        public int? SessaoId { get; set; }
        public Sessao? Sessao { get; set; }

        /// <summary>Data no mundo do jogo, em texto livre (cada mesa usa o seu calendário).</summary>
        [StringLength(60)]
        public string? RotuloData { get; set; }

        /// <summary>Posição na cronologia do mundo: menor é mais antigo.</summary>
        public int Ordem { get; set; }

        /// <summary>Segredos e spoilers: só o Mestre vê.</summary>
        public bool SoMestre { get; set; }

        /// <summary>Registrado pelo sistema (combate encerrado, descanso...) e não escrito por uma pessoa.</summary>
        public bool Automatico { get; set; }

        public int? CriadoPorId { get; set; }
        public Usuario? CriadoPor { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
