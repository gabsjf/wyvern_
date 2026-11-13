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
        /// <summary>Só vai no corpo para o app nativo; na web fica num cookie HttpOnly (Path=/Auth).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; set; }
        /// <summary>Validade do refresh token; só serve para a API gravar o cookie e nunca é serializada.</summary>
        [JsonIgnore]
        public DateTime RefreshExpiraEm { get; set; }
        /// <summary>0 = sem foto; o front usa como versão na URL da foto.</summary>
        public int FotoVersao { get; set; }
        /// <summary>Conta com vaga no plano (ou qualquer conta no beta): o front mostra o botão "Nova campanha".</summary>
        public bool PodeCriarCampanha { get; set; }
    }
}
