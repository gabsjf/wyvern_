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
        /// <summary>Nulo até o dono do e-mail clicar no link de confirmação; sem isso a conta não faz login.</summary>
        public DateTime? EmailConfirmadoEm { get; set; }
        /// <summary>SHA-256 do token do link de confirmação (o token em si só vai no e-mail).</summary>
        public string? TokenConfirmacaoHash { get; set; }
        public DateTime? TokenConfirmacaoExpiraEm { get; set; }
        /// <summary>SHA-256 do token do link de redefinição de senha; uso único e curto.</summary>
        public string? TokenRedefinicaoHash { get; set; }
        public DateTime? TokenRedefinicaoExpiraEm { get; set; }


    }
}
