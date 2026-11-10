using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Application.Services;

namespace Wyvern.Api.Controllers
{
    /// <summary>Catálogo dos planos e se o beta está ativo. Público: o cadastro e uma futura página de preços leem daqui.</summary>
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class PlanosController : ControllerBase
    {
        [HttpGet]
        public ActionResult<CatalogoDePlanosDto> Catalogo([FromServices] IPlanoService planos) => Ok(planos.Catalogo());
    }
}
