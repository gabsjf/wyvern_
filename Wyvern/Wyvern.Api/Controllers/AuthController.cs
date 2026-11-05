using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wyvern.Api.Extensions;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Application.Services;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var response = await _authService.RegisterAsync(dto);
                return IniciarSessao(response);
            }
            catch (RegraNegocioException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAsync(dto);
                return IniciarSessao(response);
            }
            catch (RegraNegocioException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>Quem está logado. É assim que o front descobre se há sessão, já que não consegue ler o cookie.</summary>
        // Fora do limite "auth": o front chama a cada carga de página.
        [HttpGet("me")]
        [Authorize]
        [DisableRateLimiting]
        public IActionResult Me()
        {
            if (!int.TryParse(User.FindFirst("nameid")?.Value, out var usuarioId))
                return Unauthorized();

            return Ok(new AuthResponseDto
            {
                UsuarioId = usuarioId,
                Nome = User.FindFirst("unique_name")?.Value ?? string.Empty,
                Email = User.FindFirst("email")?.Value ?? string.Empty,
                Papel = User.FindFirst("role")?.Value ?? "Jogador"
            });
        }

        [HttpPost("logout")]
        [DisableRateLimiting]
        public IActionResult Logout()
        {
            SessionCookie.Apagar(HttpContext);
            return NoContent();
        }

        // O token sempre vai para o cookie; só o app nativo o recebe também no corpo.
        private IActionResult IniciarSessao(AuthResponseDto response)
        {
            SessionCookie.Gravar(HttpContext, response.Token!);
            if (!SessionCookie.ClienteNativo(Request))
                response.Token = null;
            return Ok(response);
        }
    }
}
