using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Application.Services;

namespace Wyvern.Api.Controllers
{
    /// <summary>Operações de administrador (e-mails em Admin:Emails). Enquanto não há pagamento, é por aqui que uma conta muda de plano.</summary>
    [Authorize(Policy = "Admin")]
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IPlanoService _planos;

        public AdminController(IPlanoService planos) => _planos = planos;

        /// <summary>Define o plano de uma conta; a conta passa a ser Mestre.</summary>
        [HttpPut("usuarios/{usuarioId:int}/plano")]
        public async Task<ActionResult<ContaDto>> DefinirPlano(int usuarioId, DefinirPlanoDto dto) =>
            Ok(await _planos.DefinirPlanoAsync(usuarioId, dto.Plano));
    }
}
