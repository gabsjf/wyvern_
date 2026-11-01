using System;
using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Campanha
{
    public class ConviteDto
    {
        public string Token { get; set; } = string.Empty;
        /// <summary>Quando o link deixa de valer (UTC).</summary>
        public DateTime? ExpiraEm { get; set; }
    }

    public class MembroDto
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        /// <summary>"Mestre" ou "Jogador"</summary>
        public string Papel { get; set; } = string.Empty;
        public DateTime EntrouEm { get; set; }
        /// <summary>Quem criou a campanha.</summary>
        public bool EhCriador { get; set; }
        public bool EhVoce { get; set; }
    }

    public class AlterarPapelDto
    {
        /// <summary>"Mestre" ou "Jogador"</summary>
        [Required]
        public string Papel { get; set; } = string.Empty;
    }
}
