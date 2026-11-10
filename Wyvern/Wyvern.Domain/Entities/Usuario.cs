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
        /// <summary>Quando a própria pessoa excluiu a conta. A linha fica anonimizada ("Conta excluída") só para os registros compartilhados não quebrarem.</summary>
        public DateTime? ExcluidoEm { get; set; }
        /// <summary>Tipo de conta: "Mestre" (cria campanhas, conforme o plano) ou "Jogador" (só entra por convite).</summary>
        [Required]
        public string Papel { get; set; } = "Jogador";
        /// <summary>Chave do plano no catálogo da configuração ("Free", "Standard"...). Só vale para conta Mestre.</summary>
        [Required, MaxLength(40)]
        public string Plano { get; set; } = "Free";
        public DateTime? PlanoAlteradoEm { get; set; }
        /// <summary>Nulo até o dono do e-mail clicar no link de confirmação; sem isso a conta não faz login.</summary>
        public DateTime? EmailConfirmadoEm { get; set; }
        /// <summary>SHA-256 do token do link de confirmação (o token em si só vai no e-mail).</summary>
        public string? TokenConfirmacaoHash { get; set; }
        public DateTime? TokenConfirmacaoExpiraEm { get; set; }
        /// <summary>SHA-256 do token do link de redefinição de senha; uso único e curto.</summary>
        public string? TokenRedefinicaoHash { get; set; }
        public DateTime? TokenRedefinicaoExpiraEm { get; set; }
        /// <summary>E-mail novo pedido na tela de perfil; só vira o <see cref="Email"/> depois do clique no link enviado a ele.</summary>
        [MaxLength(256)]
        public string? EmailPendente { get; set; }
        /// <summary>SHA-256 do token do link de troca de e-mail.</summary>
        public string? TokenTrocaEmailHash { get; set; }
        public DateTime? TokenTrocaEmailExpiraEm { get; set; }
        /// <summary>0 = sem foto. Sobe a cada foto nova: vira o ETag e fura o cache do navegador.</summary>
        public int FotoVersao { get; set; }


    }
}
