namespace Wyvern.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
