using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Wyvern.Application.DTOs.Usuario
{
    /// <summary>O que a tela "Meu perfil" mostra da própria conta.</summary>
    public class PerfilDto
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>E-mail novo aguardando o clique no link; nulo se não há troca pendente.</summary>
        public string? EmailPendente { get; set; }
        public DateTime CriadoEm { get; set; }
        /// <summary>0 = sem foto.</summary>
        public int FotoVersao { get; set; }
        /// <summary>Tipo de conta, plano, limites e uso.</summary>
        public ContaDto Conta { get; set; } = new();
    }

    public class AlterarNomeDto
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(80, ErrorMessage = "O nome pode ter no máximo 80 caracteres")]
        public string Nome { get; set; } = string.Empty;
    }

    public class FotoFormDto
    {
        [Required(ErrorMessage = "Envie uma imagem")]
        public IFormFile? Foto { get; set; }
    }

    public class TrocarSenhaDto
    {
        [Required(ErrorMessage = "Informe a senha atual")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        public string NovaSenha { get; set; } = string.Empty;
    }

    public class TrocarEmailDto
    {
        [Required(ErrorMessage = "O novo e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [StringLength(256)]
        public string NovoEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha atual")]
        public string SenhaAtual { get; set; } = string.Empty;
    }

    /// <summary>Um aparelho conectado: uma família de refresh tokens, que nasce no login.</summary>
    public class SessaoAtivaDto
    {
        public Guid Id { get; set; }
        public string Dispositivo { get; set; } = "Aparelho desconhecido";
        public DateTime IniciadaEm { get; set; }
        public DateTime UltimaAtividadeEm { get; set; }
        /// <summary>É a sessão de quem está pedindo a lista.</summary>
        public bool Atual { get; set; }
    }
}
