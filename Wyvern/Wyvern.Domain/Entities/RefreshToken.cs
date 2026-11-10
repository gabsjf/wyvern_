using System;
using System.ComponentModel.DataAnnotations;

namespace Wyvern.Domain.Entities
{
    /// <summary>
    /// Refresh token opaco. O valor em si só existe no cliente; aqui fica o hash (SHA-256). Cada uso gera um token novo da
    /// mesma família; reusar um token já trocado indica roubo e revoga a família inteira.
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public int RefreshTokenId { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        /// <summary>Uma família por login; todos os tokens trocados em sequência a partir dele a compartilham.</summary>
        public Guid FamiliaId { get; set; }

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }

        /// <summary>Descrição do aparelho do login ("Chrome em Windows"), herdada pelos tokens da família; só para a tela de aparelhos conectados.</summary>
        [MaxLength(120)]
        public string? Dispositivo { get; set; }

        /// <summary>Quando foi trocado por um token novo (nulo = ainda não usado).</summary>
        public DateTime? UsadoEm { get; set; }

        /// <summary>Quando a família foi revogada (logout ou reuso detectado).</summary>
        public DateTime? RevogadoEm { get; set; }
    }
}
