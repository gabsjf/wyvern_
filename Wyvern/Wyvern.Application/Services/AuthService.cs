using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;
using Wyvern.Domain.Interfaces.Repositories;
using BCrypt.Net;

namespace Wyvern.Application.Services
{
    public class AuthService : IAuthService
    {
        private static readonly string[] PapeisPermitidos = { "Mestre", "Jogador" };

        private readonly IUnitOfWork _uof;
        private readonly JwtSettings _jwt;
        private readonly string[] _admins;

        public AuthService(IUnitOfWork uof, IConfiguration configuration)
        {
            _uof = uof;
            _jwt = JwtSettings.FromConfiguration(configuration);
            _admins = configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var email = dto.Email.Trim();

            var existingUser = await _uof.UsuarioRepository.GetByEmailAsync(email);
            if (existingUser != null)
                throw new RegraNegocioException("Email já está em uso.");

            var user = new Usuario
            {
                Nome = dto.Nome.Trim(),
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                Papel = NormalizarPapel(dto.Papel),
                CriadoEm = DateTime.Now,
                Ativo = true
            };

            await _uof.UsuarioRepository.CreateUsuarioAsync(user);
            await _uof.CommitAsync();

            return CriarResposta(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _uof.UsuarioRepository.GetByEmailAsync(dto.Email.Trim());
            if (user == null || !user.Ativo || !BCrypt.Net.BCrypt.Verify(dto.Senha, user.SenhaHash))
                throw new RegraNegocioException("Email ou senha inválidos.");

            return CriarResposta(user);
        }

        // O papel global é só uma preferência de interface; qualquer valor fora da lista vira "Jogador".
        private static string NormalizarPapel(string? papel)
        {
            return PapeisPermitidos.FirstOrDefault(p => string.Equals(p, papel?.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? "Jogador";
        }

        public const string ClaimTicketHub = "hub_ticket";
        private static readonly TimeSpan ValidadeTicketHub = TimeSpan.FromSeconds(60);

        // O WebSocket não manda cabeçalho, então o app nativo leva o token na URL; o que vai lá é este ticket, que expira em
        // segundos e só vale nos hubs, e não o JWT da sessão. Copia a identidade do token atual (sem consultar o banco).
        public string GerarTicketHub(ClaimsPrincipal usuario)
        {
            var claims = new List<Claim> { new Claim(ClaimTicketHub, "1") };
            AdicionarSePresente(claims, usuario, ClaimTypes.NameIdentifier, "nameid");
            AdicionarSePresente(claims, usuario, ClaimTypes.Name, "unique_name");
            AdicionarSePresente(claims, usuario, ClaimTypes.Email, "email");
            AdicionarSePresente(claims, usuario, "role", "role");
            AdicionarSePresente(claims, usuario, "admin", "admin");

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                Expires = DateTime.UtcNow.Add(ValidadeTicketHub),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        private static void AdicionarSePresente(List<Claim> claims, ClaimsPrincipal origem, string tipoNoToken, string tipoNoPrincipal)
        {
            var valor = origem.FindFirst(tipoNoPrincipal)?.Value;
            if (!string.IsNullOrEmpty(valor)) claims.Add(new Claim(tipoNoToken, valor));
        }

        private AuthResponseDto CriarResposta(Usuario user)
        {
            return new AuthResponseDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                Papel = user.Papel,
                Token = GenerateJwtToken(user)
            };
        }

        private string GenerateJwtToken(Usuario user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Papel ?? "Jogador")
            };

            // Único privilégio global: editar os catálogos compartilhados (itens, magias, perícias).
            if (_admins.Contains(user.Email, StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim("admin", "true"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
