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
        private readonly ISessaoValidator _sessoes;
        private readonly IEmailSender _email;
        private readonly string _urlFront;

        // PNG hospedado no front (public/assets/images); os e-mails não carregam imagem embutida.
        private string UrlLogo => $"{_urlFront}/assets/images/logo-dragao-email.png";

        private readonly ILimitadorDeLogin _limitador;

        // Hash de uma senha qualquer: quando o e-mail não existe, o login gasta o mesmo tempo de uma conta de verdade.
        private static readonly string HashFalso = BCrypt.Net.BCrypt.HashPassword("senha-que-ninguem-usa");

        public AuthService(IUnitOfWork uof, IConfiguration configuration, ISessaoValidator sessoes, IEmailSender email, ILimitadorDeLogin limitador)
        {
            _uof = uof;
            _sessoes = sessoes;
            _limitador = limitador;
            _email = email;
            _urlFront = (configuration["Email:UrlFront"] ?? throw new InvalidOperationException("Configure Email:UrlFront (variável Email__UrlFront)."))
                .TrimEnd('/');
            _jwt = JwtSettings.FromConfiguration(configuration);
            _admins = configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            var email = dto.Email.Trim();

            // O hash é calculado sempre, antes de consultar o e-mail: o tempo da resposta não revela se a conta existe.
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

            var existente = await _uof.UsuarioRepository.GetByEmailAsync(email);
            if (existente == null)
            {
                var user = new Usuario
                {
                    Nome = dto.Nome.Trim(),
                    Email = email,
                    SenhaHash = senhaHash,
                    Papel = NormalizarPapel(dto.Papel),
                    CriadoEm = DateTime.Now,
                    Ativo = true
                };
                await _uof.UsuarioRepository.CreateUsuarioAsync(user);
                await _uof.CommitAsync(); // o token leva o id do usuário, que só existe depois daqui
                await EnviarConfirmacaoAsync(user);
            }
            else if (existente.EmailConfirmadoEm == null)
            {
                // Quem cadastrou primeiro não confirmou: manda outro link, mas sem trocar nome nem senha. Se aceitasse os
                // dados novos, quem digitasse o e-mail alheio escolheria a senha da conta que o dono vai confirmar.
                await EnviarConfirmacaoAsync(existente);
            }
            else
            {
                var corpo = ModeloDeEmail.ContaJaExiste(existente.Nome, $"{_urlFront}/login", UrlLogo);
                await _email.EnviarAsync(existente.Email, "Alguém tentou criar uma conta com o seu e-mail - Wyvern", corpo.Texto, corpo.Html);
            }
        }

        public async Task ConfirmarEmailAsync(string token)
        {
            var invalido = new RegraNegocioException("Link de confirmação inválido ou expirado.");

            var user = await UsuarioDoTokenAsync(token);
            if (user == null || !TokenConfere(token, user.TokenConfirmacaoHash, user.TokenConfirmacaoExpiraEm))
                throw invalido;

            user.EmailConfirmadoEm = DateTime.UtcNow;
            user.TokenConfirmacaoHash = null;
            user.TokenConfirmacaoExpiraEm = null;
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();
        }

        /// <summary>
        /// Esqueci a senha. Não revela se o e-mail tem conta: quem tem recebe o link (válido por 1 h, uso único); conta ainda
        /// não confirmada recebe o link de confirmação, e e-mail sem conta não recebe nada.
        /// </summary>
        public async Task SolicitarRedefinicaoDeSenhaAsync(string email)
        {
            var user = await _uof.UsuarioRepository.GetByEmailAsync(email.Trim());
            if (user == null) return;

            if (user.EmailConfirmadoEm == null)
            {
                await EnviarConfirmacaoAsync(user);
                return;
            }

            var token = NovoToken(user.UsuarioId);
            user.TokenRedefinicaoHash = HashDoToken(token);
            user.TokenRedefinicaoExpiraEm = DateTime.UtcNow.AddHours(1);
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            var corpo = ModeloDeEmail.Redefinicao(user.Nome, $"{_urlFront}/redefinir-senha?token={Uri.EscapeDataString(token)}", UrlLogo);
            await _email.EnviarAsync(user.Email, "Redefinição de senha - Wyvern", corpo.Texto, corpo.Html);
        }

        /// <summary>Troca a senha com o token do e-mail e derruba todas as sessões abertas da conta.</summary>
        public async Task RedefinirSenhaAsync(string token, string novaSenha)
        {
            var user = await UsuarioDoTokenAsync(token);
            if (user == null || !TokenConfere(token, user.TokenRedefinicaoHash, user.TokenRedefinicaoExpiraEm))
                throw new RegraNegocioException("Link de redefinição inválido ou expirado.");

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            user.TokenRedefinicaoHash = null;
            user.TokenRedefinicaoExpiraEm = null;
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            // Quem provou ter o e-mail não fica preso a um bloqueio de tentativas anteriores.
            _limitador.Zerar(user.Email);

            // Quem roubou a senha antiga pode estar logado: encerra tudo, inclusive os access tokens em cache.
            foreach (var familiaId in await _uof.RefreshTokenRepository.RevogarTodasAsync(user.UsuarioId, DateTime.UtcNow))
                _sessoes.Invalidar(familiaId);
        }

        // Tokens têm a forma "id.aleatório"; só o hash fica no banco. Pedir outro link invalida o anterior.
        private static string NovoToken(int usuarioId) =>
            $"{usuarioId}." + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private async Task<Usuario?> UsuarioDoTokenAsync(string token)
        {
            var ponto = token.IndexOf('.');
            if (ponto <= 0 || !int.TryParse(token.AsSpan(0, ponto), out var usuarioId))
                return null;
            return await _uof.UsuarioRepository.GetUsuarioAsync(usuarioId);
        }

        private static bool TokenConfere(string token, string? hashSalvo, DateTime? expiraEm) =>
            hashSalvo != null && expiraEm != null && expiraEm > DateTime.UtcNow
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hashSalvo), Encoding.ASCII.GetBytes(HashDoToken(token)));

        // Link de confirmação, válido por 24 h.
        private async Task EnviarConfirmacaoAsync(Usuario user)
        {
            var token = NovoToken(user.UsuarioId);
            user.TokenConfirmacaoHash = HashDoToken(token);
            user.TokenConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(24);
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            var corpo = ModeloDeEmail.Confirmacao(user.Nome, $"{_urlFront}/confirmar-email?token={Uri.EscapeDataString(token)}", UrlLogo);
            await _email.EnviarAsync(user.Email, "Confirme o seu e-mail - Wyvern", corpo.Texto, corpo.Html);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var email = dto.Email.Trim();

            // Bloqueado: nem confere a senha, para o atacante não testar palpites durante o bloqueio.
            if (_limitador.TempoRestanteDoBloqueio(email) is { } espera)
                throw new LoginBloqueadoException(espera);

            var user = await _uof.UsuarioRepository.GetByEmailAsync(email);
            var senhaConfere = BCrypt.Net.BCrypt.Verify(dto.Senha, user?.SenhaHash ?? HashFalso);
            if (user == null || !user.Ativo || !senhaConfere)
            {
                _limitador.RegistrarFalha(email); // conta existente ou não: o contador é por e-mail digitado
                throw new RegraNegocioException("Email ou senha inválidos.");
            }

            _limitador.Zerar(email);
            // Só depois de a senha conferir: quem não tem a senha não descobre nada sobre a conta.
            if (user.EmailConfirmadoEm == null)
                throw new RegraNegocioException("Confirme o seu e-mail para entrar. Enviamos um link para a sua caixa de entrada.");

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
                    await RevogarFamiliaAsync(atual.FamiliaId, agora);
                throw SessaoExpirada();
            }

            var user = await _uof.UsuarioRepository.GetUsuarioAsync(atual.UsuarioId);
            if (user == null || !user.Ativo)
            {
                await RevogarFamiliaAsync(atual.FamiliaId, agora);
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
                await RevogarFamiliaAsync(atual.FamiliaId, DateTime.UtcNow);
        }

        // Revoga no banco e esquece o cache de sessão ativa, para os access tokens dessa família pararem de valer já.
        private async Task RevogarFamiliaAsync(Guid familiaId, DateTime agora)
        {
            await _uof.RefreshTokenRepository.RevogarFamiliaAsync(familiaId, agora);
            _sessoes.Invalidar(familiaId);
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
        public const string SidClaim = "sid";
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
            AdicionarSePresente(claims, usuario, SidClaim, SidClaim);

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
                Token = GenerateJwtToken(user, familiaId),
                RefreshToken = refreshToken,
                RefreshExpiraEm = expiraEm
            };
        }

        // "sid" liga o token à família do refresh token: é por ela que o servidor sabe se a sessão foi revogada.
        private string GenerateJwtToken(Usuario user, Guid sessaoId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Papel ?? "Jogador"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")), // cada emissão é um token distinto
                new Claim(SidClaim, sessaoId.ToString())
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
