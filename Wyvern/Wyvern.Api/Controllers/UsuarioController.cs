using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wyvern.Api.Extensions;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Application.Services;
using Wyvern.Domain.Exceptions;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    /// <summary>
    /// Operações sobre a própria conta (tela "Meu perfil"). As rotas "me" tiram o usuário da sessão, então não há como
    /// mirar a conta de outra pessoa. O cadastro é feito em <see cref="AuthController"/>.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        private const long TamanhoMaximoFoto = 2 * 1024 * 1024;

        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuthService _auth;
        private readonly IPlanoService _planos;

        public UsuarioController(IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUser, IAuthService auth, IPlanoService planos)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUser = currentUser;
            _auth = auth;
            _planos = planos;
        }

        private bool IsProprioUsuario(int id) => _currentUser.UserId == id;

        private int UsuarioId => _currentUser.UserId ?? throw new UnauthorizedAccessException();

        private Guid? SessaoAtual => Guid.TryParse(User.FindFirst(AuthService.SidClaim)?.Value, out var sid) ? sid : null;

        // ---------- perfil ----------

        [HttpGet("me")]
        public async Task<ActionResult<PerfilDto>> Perfil()
        {
            var user = await _uof.UsuarioRepository.GetUsuarioAsync(UsuarioId);
            if (user == null) return NotFound("Usuário não encontrado");

            return Ok(new PerfilDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                EmailPendente = user.EmailPendente,
                CriadoEm = user.CriadoEm,
                FotoVersao = user.FotoVersao,
                Conta = await _planos.ContaAsync(user.UsuarioId)
            });
        }

        [HttpPut("me/nome")]
        public async Task<ActionResult<PerfilDto>> AlterarNome(AlterarNomeDto dto)
        {
            var nome = dto.Nome.Trim();
            if (nome.Length == 0) return BadRequest(new { message = "O nome é obrigatório." });

            var user = await _uof.UsuarioRepository.GetUsuarioAsync(UsuarioId);
            if (user == null) return NotFound("Usuário não encontrado");

            user.Nome = nome;
            await _uof.CommitAsync();
            return await Perfil();
        }

        // ---------- foto ----------

        [HttpPut("me/foto")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanhoMaximoFoto + 64 * 1024)]
        [EnableRateLimiting("upload")]
        public async Task<ActionResult<PerfilDto>> EnviarFoto([FromForm] FotoFormDto form)
        {
            var arquivo = form.Foto!;
            if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximoFoto)
                return BadRequest(new { message = "A foto precisa ter até 2 MB." });

            byte[] bytes;
            using (var ms = new MemoryStream((int)arquivo.Length))
            {
                await arquivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            var tipo = TiposDeImagem.Identificar(bytes);
            if (tipo == null) return BadRequest(new { message = "Use uma imagem PNG, JPG ou WebP." });

            var user = await _uof.UsuarioRepository.GetUsuarioAsync(UsuarioId);
            if (user == null) return NotFound("Usuário não encontrado");

            await _uof.UsuarioRepository.SalvarFotoAsync(user.UsuarioId, tipo, bytes);
            user.FotoVersao = Math.Max(user.FotoVersao, 0) + 1;
            await _uof.CommitAsync();
            return await Perfil();
        }

        [HttpDelete("me/foto")]
        public async Task<ActionResult<PerfilDto>> RemoverFoto()
        {
            var user = await _uof.UsuarioRepository.GetUsuarioAsync(UsuarioId);
            if (user == null) return NotFound("Usuário não encontrado");

            await _uof.UsuarioRepository.RemoverFotoAsync(user.UsuarioId);
            user.FotoVersao = 0;
            await _uof.CommitAsync();
            return await Perfil();
        }

        [HttpGet("me/foto")]
        public async Task<IActionResult> Foto()
        {
            var user = await _uof.UsuarioRepository.GetResumoAsync(UsuarioId);
            if (user == null || user.FotoVersao == 0) return NotFound("Sem foto.");

            var etag = $"\"{user.UsuarioId}-{user.FotoVersao}\"";
            if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

            var foto = await _uof.UsuarioRepository.GetFotoAsync(user.UsuarioId);
            if (foto == null) return NotFound("Sem foto.");

            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, no-cache";
            return File(foto.Dados, foto.Tipo);
        }

        // ---------- senha e e-mail ----------

        [HttpPost("me/senha")]
        [EnableRateLimiting("auth")]
        public Task<IActionResult> TrocarSenha(TrocarSenhaDto dto) => Executar(async () =>
        {
            await _auth.TrocarSenhaAsync(UsuarioId, SessaoAtual, dto.SenhaAtual, dto.NovaSenha);
            return NoContent();
        });

        [HttpPost("me/email")]
        [EnableRateLimiting("auth")]
        public Task<IActionResult> TrocarEmail(TrocarEmailDto dto) => Executar(async () =>
        {
            await _auth.SolicitarTrocaDeEmailAsync(UsuarioId, dto.NovoEmail, dto.SenhaAtual);
            return Accepted(new { message = "Enviamos um link para o novo e-mail. A troca só vale depois que você clicar nele." });
        });

        // ---------- aparelhos conectados ----------

        [HttpGet("me/sessoes")]
        public async Task<ActionResult<List<SessaoAtivaDto>>> Sessoes() =>
            Ok(await _auth.ListarSessoesAsync(UsuarioId, SessaoAtual));

        [HttpDelete("me/sessoes/{sessaoId:guid}")]
        public Task<IActionResult> EncerrarSessao(Guid sessaoId) => Executar(async () =>
        {
            if (SessaoAtual is not { } atual) return Unauthorized();
            return await _auth.EncerrarSessaoDoUsuarioAsync(UsuarioId, atual, sessaoId)
                ? NoContent()
                : NotFound(new { message = "Sessão não encontrada." });
        });

        [HttpDelete("me/sessoes")]
        public async Task<IActionResult> EncerrarOutrasSessoes()
        {
            if (SessaoAtual is not { } atual) return Unauthorized();
            await _auth.EncerrarOutrasSessoesAsync(UsuarioId, atual);
            return NoContent();
        }

        // ---------- exclusão de conta ----------

        /// <summary>O que acontece com as campanhas da pessoa se ela excluir a conta agora.</summary>
        [HttpGet("me/exclusao")]
        public async Task<ActionResult<PreviaDeExclusaoDto>> PreviaDeExclusao() =>
            Ok(await _auth.PreviaDeExclusaoAsync(UsuarioId));

        /// <summary>Exclui a conta na hora (senha + "EXCLUIR"). As sessões caem e os cookies são apagados.</summary>
        [HttpPost("me/excluir")]
        [EnableRateLimiting("auth")]
        public Task<IActionResult> ExcluirConta(ExcluirContaDto dto) => Executar(async () =>
        {
            if (!string.Equals(dto.Confirmacao?.Trim(), ExcluirContaDto.PalavraDeConfirmacao, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Digite {ExcluirContaDto.PalavraDeConfirmacao} para confirmar." });

            await _auth.ExcluirContaAsync(UsuarioId, dto.Senha);
            SessionCookie.Apagar(HttpContext);
            SessionCookie.ApagarRefresh(HttpContext);
            return NoContent();
        });

        private async Task<IActionResult> Executar(Func<Task<IActionResult>> acao)
        {
            try
            {
                return await acao();
            }
            catch (LoginBloqueadoException ex)
            {
                Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.Espera.TotalSeconds)).ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            catch (RegraNegocioException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ---------- por id (só a própria conta) ----------

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUserById(int id)
        {
            if (!IsProprioUsuario(id)) return Forbid();

            var user = await _uof.UsuarioRepository.GetUsuarioAsync(id);
            if (user == null)
            {
                return NotFound("Usuário não encontrado");
            }
            return Ok(_mapper.Map<UsuarioResponseDto>(user));
        }

    }
}
