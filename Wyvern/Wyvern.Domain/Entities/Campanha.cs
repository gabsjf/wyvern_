using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Wyvern.Domain.Entities
{
    public class Campanha
    {
        [Key]
        public int CampanhaId { get; set; }
        [Required]
        public string Nome { get; set; }
        public string Sistema { get; set; }
        public DateTime CriadoEm { get; set; }

        /// <summary>Quem criou a campanha. As permissões vêm de <see cref="Membros"/>, não deste campo.</summary>
        public int MestreId { get; set; }
        public Usuario? Mestre { get; set; }
        public List<Sessao>? Sessoes { get; set; }
        public bool Ativo { get; set; } = true;
        public string? TokenConvite { get; set; }
        /// <summary>Quando o link de convite atual deixa de valer (UTC). Nulo em convites antigos, que não expiram.</summary>
        public DateTime? ConviteExpiraEm { get; set; }
        public List<CampanhaMembro>? Membros { get; set; }


    }
}
