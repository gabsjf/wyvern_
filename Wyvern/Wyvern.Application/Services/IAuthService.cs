using System.Threading.Tasks;
using Wyvern.Application.DTOs.Auth;

namespace Wyvern.Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
