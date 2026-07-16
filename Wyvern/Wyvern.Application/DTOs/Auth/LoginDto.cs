using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "O Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A Senha é obrigatória")]
        public string Senha { get; set; }
    }
}
