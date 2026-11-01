using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    /// <summary>
    /// Operações sobre a própria conta. O cadastro é feito em <see cref="AuthController"/>;
    /// não existe listagem nem manipulação de contas de outros usuários.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public UsuarioController(IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUser)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        private bool IsProprioUsuario(int id) => _currentUser.UserId == id;

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

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateUser(int id, UsuarioUpdateDto usuarioDto)
        {
            if (!IsProprioUsuario(id)) return Forbid();

            var usuarioBanco = await _uof.UsuarioRepository.GetUsuarioAsync(id);
            if (usuarioBanco == null)
            {
                return NotFound("Usuário não encontrado");
            }

            var emailEmUso = await _uof.UsuarioRepository.GetByEmailAsync(usuarioDto.Email.Trim());
            if (emailEmUso != null && emailEmUso.UsuarioId != id)
            {
                return BadRequest(new { message = "Email já está em uso." });
            }

            _mapper.Map(usuarioDto, usuarioBanco);
            await _uof.UsuarioRepository.UpdateUsuarioAsync(usuarioBanco);
            await _uof.CommitAsync();
            return Ok(_mapper.Map<UsuarioResponseDto>(usuarioBanco));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (!IsProprioUsuario(id)) return Forbid();

            var user = await _uof.UsuarioRepository.DeleteUsuarioAsync(id);
            if (user == null)
            {
                return NotFound("Usuário não encontrado");
            }
            await _uof.CommitAsync();
            return NoContent();
        }
    }
}
