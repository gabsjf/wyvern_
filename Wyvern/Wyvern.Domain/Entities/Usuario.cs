using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wyvern.Domain.Entities
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }
        [Required]
        public string Nome { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string SenhaHash { get; set; }
        public DateTime CriadoEm { get; set; }
        public List<Campanha>? Campanhas { get; set; }
        public bool Ativo { get; set; } = true;
        [Required]
        public string Papel { get; set; } = "Jogador";


    }
}
