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
            // Mesma resposta para e-mail novo e já cadastrado, e sem sessão: o resultado chega por e-mail ao dono.
            await _authService.RegisterAsync(dto);
            return Accepted(new { message = "Se o e-mail informado puder ser usado, enviamos um link de confirmação para ele. Verifique a sua caixa de entrada." });
        }

        [HttpPost("confirmar-email")]
        public async Task<IActionResult> ConfirmarEmail([FromBody] ConfirmarEmailDto dto)
        {
            try
            {
                await _authService.ConfirmarEmailAsync(dto.Token);
                return NoContent();
            }
            catch (RegraNegocioException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("esqueci-senha")]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDto dto)
        {
            // Mesma resposta exista a conta ou não.
            await _authService.SolicitarRedefinicaoDeSenhaAsync(dto.Email);
            return Accepted(new { message = "Se existir uma conta com esse e-mail, enviamos um link para redefinir a senha. Verifique a sua caixa de entrada." });
        }

        [HttpPost("redefinir-senha")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto)
        {
            try
            {
                await _authService.RedefinirSenhaAsync(dto.Token, dto.NovaSenha);
                return NoContent();
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
            catch (LoginBloqueadoException ex)
            {
                Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.Espera.TotalSeconds)).ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
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

        /// <summary>
        /// Ticket de ~60 s para abrir um hub. O app nativo o usa no lugar do JWT da sessão, que de outro modo iria na URL do WebSocket.
        /// </summary>
        // Fora do limite "auth": cada negociação e reconexão de hub pede um ticket novo.
        [HttpPost("hub-ticket")]
        [Authorize]
        [DisableRateLimiting]
        public IActionResult TicketHub() => Ok(new { ticket = _authService.GerarTicketHub(User) });

        /// <summary>
        /// Troca o refresh token (cookie na web, corpo no app nativo) por um access token novo e um refresh novo.
        /// Token inválido, vencido ou reusado: 401 e os cookies são apagados.
        /// </summary>
        // O refresh token tem 256 bits aleatórios, então não há o que forçar por tentativa; e um 429 aqui deslogaria o usuário.
        [HttpPost("refresh")]
        [DisableRateLimiting]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto? dto)
        {
            try
            {
                return IniciarSessao(await _authService.RefreshAsync(TokenDeRefresh(dto)));
            }
            catch (RegraNegocioException ex)
            {
                SessionCookie.Apagar(HttpContext);
                SessionCookie.ApagarRefresh(HttpContext);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [DisableRateLimiting]
        public async Task<IActionResult> Logout([FromBody] RefreshDto? dto)
        {
            await _authService.EncerrarSessaoAsync(TokenDeRefresh(dto));
            SessionCookie.Apagar(HttpContext);
            SessionCookie.ApagarRefresh(HttpContext);
            return NoContent();
        }

        private string? TokenDeRefresh(RefreshDto? dto) =>
            SessionCookie.ClienteNativo(Request) ? dto?.RefreshToken : Request.Cookies[SessionCookie.NomeRefresh];

        // Os tokens sempre vão para cookies; só o app nativo os recebe também no corpo.
        private IActionResult IniciarSessao(AuthResponseDto response)
        {
            SessionCookie.Gravar(HttpContext, response.Token!);
            SessionCookie.GravarRefresh(HttpContext, response.RefreshToken!, response.RefreshExpiraEm);
            if (!SessionCookie.ClienteNativo(Request))
            {
                response.Token = null;
                response.RefreshToken = null;
            }
            return Ok(response);
        }
    }
}
