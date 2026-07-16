using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using BCrypt.Net;

namespace Wyvern.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uof;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork uof, IConfiguration configuration)
        {
            _uof = uof;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Verify if email exists
            var existingUser = await _uof.UsuarioRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("Email já está em uso.");

            var user = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                CriadoEm = DateTime.Now,
                Ativo = true
            };

            await _uof.UsuarioRepository.CreateUsuarioAsync(user);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                Token = token
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _uof.UsuarioRepository.GetByEmailAsync(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, user.SenhaHash))
                throw new Exception("Email ou senha inválidos.");

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                Token = token
            };
        }

        private string GenerateJwtToken(Usuario user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "WyvernSuperSecretKey1234567890!!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, user.Nome),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
