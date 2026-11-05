using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private static readonly TimeSpan ToleranciaReuso = TimeSpan.FromSeconds(10);

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

            return await CriarRespostaAsync(user, Guid.NewGuid());
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _uof.UsuarioRepository.GetByEmailAsync(dto.Email.Trim());
            if (user == null || !user.Ativo || !BCrypt.Net.BCrypt.Verify(dto.Senha, user.SenhaHash))
                throw new RegraNegocioException("Email ou senha inválidos.");

            return await CriarRespostaAsync(user, Guid.NewGuid());
        }

        /// <summary>
        /// Troca o refresh token por um access token novo e um refresh novo da mesma família (rotação). Um token já trocado
        /// que volta depois da tolerância é reuso, sinal de roubo: a família inteira é revogada.
        /// </summary>
        public async Task<AuthResponseDto> RefreshAsync(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw SessaoExpirada();

            var agora = DateTime.UtcNow;
            var atual = await _uof.RefreshTokenRepository.GetByHashAsync(HashDoToken(refreshToken));
            if (atual == null || atual.RevogadoEm != null || atual.ExpiraEm <= agora)
                throw SessaoExpirada();

            // Sem o UPDATE atômico, duas trocas simultâneas do mesmo token bifurcariam a família.
            var trocouAgora = atual.UsadoEm == null && await _uof.RefreshTokenRepository.MarcarComoUsadoAsync(atual.RefreshTokenId, agora);
            if (!trocouAgora)
            {
                // Dentro da tolerância é quase certo uma corrida legítima (duas abas, rede ruim): só nega, sem derrubar a sessão.
                var usadoEm = atual.UsadoEm ?? agora;
                if (agora - usadoEm > ToleranciaReuso)
                    await _uof.RefreshTokenRepository.RevogarFamiliaAsync(atual.FamiliaId, agora);
                throw SessaoExpirada();
            }

            var user = await _uof.UsuarioRepository.GetUsuarioAsync(atual.UsuarioId);
            if (user == null || !user.Ativo)
            {
                await _uof.RefreshTokenRepository.RevogarFamiliaAsync(atual.FamiliaId, agora);
                throw SessaoExpirada();
            }

            await _uof.RefreshTokenRepository.ApagarExpiradosAsync(user.UsuarioId, agora);
            return await CriarRespostaAsync(user, atual.FamiliaId);
        }

        /// <summary>Logout: revoga a família do refresh token apresentado (se for válido), para ele não servir mais.</summary>
        public async Task EncerrarSessaoAsync(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var atual = await _uof.RefreshTokenRepository.GetByHashAsync(HashDoToken(refreshToken));
            if (atual != null)
                await _uof.RefreshTokenRepository.RevogarFamiliaAsync(atual.FamiliaId, DateTime.UtcNow);
        }

        private static RegraNegocioException SessaoExpirada() => new("Sessão expirada. Entre novamente.");

        private static string HashDoToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

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

        private async Task<AuthResponseDto> CriarRespostaAsync(Usuario user, Guid familiaId)
        {
            var agora = DateTime.UtcNow;
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var expiraEm = agora.AddDays(_jwt.RefreshDays);

            _uof.RefreshTokenRepository.Add(new RefreshToken
            {
                UsuarioId = user.UsuarioId,
                FamiliaId = familiaId,
                TokenHash = HashDoToken(refreshToken),
                CriadoEm = agora,
                ExpiraEm = expiraEm
            });
            await _uof.CommitAsync();

            return new AuthResponseDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                Papel = user.Papel,
                Token = GenerateJwtToken(user),
                RefreshToken = refreshToken,
                RefreshExpiraEm = expiraEm
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
                new Claim("role", user.Papel ?? "Jogador"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")) // cada emissão é um token distinto
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
