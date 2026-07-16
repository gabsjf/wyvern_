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
        public int MestreId { get; set; }
        public Usuario? Mestre { get; set; }
        public List<Sessao>? Sessoes { get; set; }
        public bool Ativo { get; set; } = true;


    }
}
