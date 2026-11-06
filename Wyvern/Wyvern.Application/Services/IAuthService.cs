using System.Security.Claims;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Auth;

namespace Wyvern.Application.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Resposta idêntica com o e-mail novo ou já cadastrado: ninguém descobre quem tem conta. Não abre sessão; o dono do
        /// e-mail recebe uma mensagem (confirmação, se é novo; aviso, se já existia).
        /// </summary>
        Task RegisterAsync(RegisterDto dto);
        Task ConfirmarEmailAsync(string token);

        /// <summary>Sempre termina sem erro, exista a conta ou não; o link vai por e-mail.</summary>
        Task SolicitarRedefinicaoDeSenhaAsync(string email);
        Task RedefinirSenhaAsync(string token, string novaSenha);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        /// <summary>Rotação: devolve access + refresh novos; reuso de um refresh já trocado revoga a família.</summary>
        Task<AuthResponseDto> RefreshAsync(string? refreshToken);
        Task EncerrarSessaoAsync(string? refreshToken);

        /// <summary>Token curto, só para abrir a conexão de um hub; não serve na API REST.</summary>
        string GerarTicketHub(ClaimsPrincipal usuario);
    }
}
