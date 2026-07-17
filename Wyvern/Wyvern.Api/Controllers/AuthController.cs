using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Wyvern.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly Wyvern.Infrastructure.Data.WyvernDbContext _db;

        public AuthController(IAuthService authService, Wyvern.Infrastructure.Data.WyvernDbContext db)
        {
            _authService = authService;
            _db = db;
        }

        [HttpGet("fix-db")]
        public IActionResult FixDb()
        {
            try
            {
                _db.Database.ExecuteSqlRaw("ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"Papel\" text NOT NULL DEFAULT 'Jogador';");
                return Ok("Fixed");
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var response = await _authService.RegisterAsync(dto);
                return Ok(response);
            }
            catch (System.Exception ex)
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
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
