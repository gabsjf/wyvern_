using System.Text.Json.Serialization;

namespace Wyvern.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        /// <summary>Só vai no corpo para o app nativo; na web o token fica num cookie HttpOnly e nunca chega ao JavaScript.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }
        public string Papel { get; set; }
    }
}
