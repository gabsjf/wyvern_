using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Auth
{
    public class ConfirmarEmailDto
    {
        [Required(ErrorMessage = "O token é obrigatório")]
        public string Token { get; set; }
    }
}
