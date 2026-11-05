using System.Security.Claims;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Auth;

namespace Wyvern.Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        /// <summary>Token curto, só para abrir a conexão de um hub; não serve na API REST.</summary>
        string GerarTicketHub(ClaimsPrincipal usuario);
    }
}
