using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Auth
{
    public class EsqueciSenhaDto
    {
        [Required(ErrorMessage = "O Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }
    }

    public class RedefinirSenhaDto
    {
        [Required(ErrorMessage = "O token é obrigatório")]
        public string Token { get; set; }

        [Required(ErrorMessage = "A Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        public string NovaSenha { get; set; }
    }
}
