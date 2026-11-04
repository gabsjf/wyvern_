using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "O Nome é obrigatório")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        public string Senha { get; set; }

        // Apenas preferência de interface ("Mestre" ou "Jogador"). Não concede nenhuma permissão:
        // a autorização é sempre decidida pela relação do usuário com cada campanha.
        public string? Papel { get; set; }
    }
}
