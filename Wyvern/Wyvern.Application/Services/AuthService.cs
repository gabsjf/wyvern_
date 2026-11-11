using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;
using Wyvern.Domain.Regras;
using Wyvern.Domain.Interfaces.Repositories;
using BCrypt.Net;

namespace Wyvern.Application.Services
{
    public class AuthService : IAuthService
    {
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

        private readonly IPlanoService _planos;

        public AuthService(IUnitOfWork uof, IConfiguration configuration, ISessaoValidator sessoes, IEmailSender email, ILimitadorDeLogin limitador, IPlanoService planos)
        {
            _uof = uof;
            _planos = planos;
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
            var email = EmailDaConta.Normalizar(dto.Email);

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
                    // Tipo de conta: no beta, todo mundo é Mestre.
                    Papel = _planos.TipoNoCadastro(dto.Papel),
                    Plano = PlanosOptions.Gratuito,
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
            var user = await _uof.UsuarioRepository.GetByEmailAsync(EmailDaConta.Normalizar(email));
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

        public async Task ReenviarConfirmacaoAsync(string email)
        {
            var user = await _uof.UsuarioRepository.GetByEmailAsync(EmailDaConta.Normalizar(email));
            if (user == null || user.EmailConfirmadoEm != null) return;

            // Um link por minuto, no máximo: o botão não vira um jeito de lotar a caixa de alguém.
            var enviadoEm = user.TokenConfirmacaoExpiraEm - ValidadeDaConfirmacao;
            if (enviadoEm > DateTime.UtcNow - IntervaloEntreReenvios) return;

            await EnviarConfirmacaoAsync(user);
        }

        private static readonly TimeSpan ValidadeDaConfirmacao = TimeSpan.FromHours(24);
        private static readonly TimeSpan IntervaloEntreReenvios = TimeSpan.FromMinutes(1);

        // Link de confirmação, válido por 24 h. Um link novo invalida o anterior.
        private async Task EnviarConfirmacaoAsync(Usuario user)
        {
            var token = NovoToken(user.UsuarioId);
            user.TokenConfirmacaoHash = HashDoToken(token);
            user.TokenConfirmacaoExpiraEm = DateTime.UtcNow + ValidadeDaConfirmacao;
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            var corpo = ModeloDeEmail.Confirmacao(user.Nome, $"{_urlFront}/confirmar-email?token={Uri.EscapeDataString(token)}", UrlLogo);
            await _email.EnviarAsync(user.Email, "Confirme o seu e-mail - Wyvern", corpo.Texto, corpo.Html);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? dispositivo = null)
        {
            var email = EmailDaConta.Normalizar(dto.Email);

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
                throw new EmailNaoConfirmadoException();

            return await CriarRespostaAsync(user, Guid.NewGuid(), dispositivo);
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
            return await CriarRespostaAsync(user, atual.FamiliaId, atual.Dispositivo);
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

        // ---------- tela "Meu perfil" ----------

        public async Task TrocarSenhaAsync(int usuarioId, Guid? sessaoAtual, string senhaAtual, string novaSenha)
        {
            var user = await ConferirSenhaAtualAsync(usuarioId, senhaAtual);

            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            // Se alguém mais tinha a senha antiga e está logado, cai agora; quem trocou continua onde está.
            var agora = DateTime.UtcNow;
            foreach (var familiaId in await _uof.RefreshTokenRepository.RevogarOutrasAsync(user.UsuarioId, sessaoAtual ?? Guid.Empty, agora))
                _sessoes.Invalidar(familiaId);
        }

        public async Task SolicitarTrocaDeEmailAsync(int usuarioId, string novoEmail, string senhaAtual)
        {
            var user = await ConferirSenhaAtualAsync(usuarioId, senhaAtual);

            var email = EmailDaConta.Normalizar(novoEmail);
            if (string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
                throw new RegraNegocioException("Esse já é o e-mail da sua conta.");

            var token = NovoToken(user.UsuarioId);
            user.EmailPendente = email;
            user.TokenTrocaEmailHash = HashDoToken(token);
            user.TokenTrocaEmailExpiraEm = DateTime.UtcNow.AddHours(24);
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            // E-mail de outra conta: tudo igual por fora, mas o link não sai. Assim a tela não vira um jeito de
            // descobrir quem tem conta, e o dono daquele endereço não recebe nada estranho.
            if (await _uof.UsuarioRepository.EmailEmUsoPorOutraContaAsync(email, user.UsuarioId))
                return;

            var corpo = ModeloDeEmail.TrocaDeEmail(user.Nome, $"{_urlFront}/confirmar-troca-email?token={Uri.EscapeDataString(token)}", UrlLogo);
            await _email.EnviarAsync(email, "Confirme o novo e-mail - Wyvern", corpo.Texto, corpo.Html);
        }

        public async Task ConfirmarTrocaDeEmailAsync(string token)
        {
            var user = await UsuarioDoTokenAsync(token);
            if (user == null || user.EmailPendente == null || !TokenConfere(token, user.TokenTrocaEmailHash, user.TokenTrocaEmailExpiraEm))
                throw new RegraNegocioException("Link de troca de e-mail inválido ou expirado.");

            var novo = user.EmailPendente;
            if (await _uof.UsuarioRepository.EmailEmUsoPorOutraContaAsync(novo, user.UsuarioId))
                throw new RegraNegocioException("Esse e-mail passou a ser usado por outra conta. Peça a troca de novo com outro endereço.");

            var antigo = user.Email;
            user.Email = novo;
            user.EmailPendente = null;
            user.TokenTrocaEmailHash = null;
            user.TokenTrocaEmailExpiraEm = null;
            await _uof.UsuarioRepository.UpdateUsuarioAsync(user);
            await _uof.CommitAsync();

            // O bloqueio de tentativas é por e-mail digitado: o novo começa limpo e o antigo não serve mais.
            _limitador.Zerar(antigo);
            _limitador.Zerar(novo);

            var corpo = ModeloDeEmail.EmailAlterado(user.Nome, MascararEmail(novo), $"{_urlFront}/login", UrlLogo);
            await _email.EnviarAsync(antigo, "O e-mail da sua conta foi alterado - Wyvern", corpo.Texto, corpo.Html);
        }

        public async Task<List<SessaoAtivaDto>> ListarSessoesAsync(int usuarioId, Guid? sessaoAtual)
        {
            var tokens = await _uof.RefreshTokenRepository.ListarAtivosAsync(usuarioId, DateTime.UtcNow);
            return tokens
                .GroupBy(t => t.FamiliaId)
                .Select(g =>
                {
                    var maisRecente = g.MaxBy(t => t.CriadoEm)!;
                    return new SessaoAtivaDto
                    {
                        Id = g.Key,
                        Dispositivo = string.IsNullOrWhiteSpace(maisRecente.Dispositivo) ? "Aparelho desconhecido" : maisRecente.Dispositivo,
                        IniciadaEm = g.Min(t => t.CriadoEm),
                        UltimaAtividadeEm = maisRecente.CriadoEm,
                        Atual = g.Key == sessaoAtual
                    };
                })
                .OrderByDescending(s => s.Atual)
                .ThenByDescending(s => s.UltimaAtividadeEm)
                .ToList();
        }

        public async Task<bool> EncerrarSessaoDoUsuarioAsync(int usuarioId, Guid sessaoAtual, Guid sessaoId)
        {
            if (sessaoId == sessaoAtual)
                throw new RegraNegocioException("Para encerrar a sessão deste aparelho, use \"Sair\".");
            if (!await _uof.RefreshTokenRepository.FamiliaDoUsuarioAsync(usuarioId, sessaoId))
                return false;

            await RevogarFamiliaAsync(sessaoId, DateTime.UtcNow);
            return true;
        }

        public async Task EncerrarOutrasSessoesAsync(int usuarioId, Guid sessaoAtual)
        {
            foreach (var familiaId in await _uof.RefreshTokenRepository.RevogarOutrasAsync(usuarioId, sessaoAtual, DateTime.UtcNow))
                _sessoes.Invalidar(familiaId);
        }

        // ---------- exclusão de conta ----------

        public async Task<PreviaDeExclusaoDto> PreviaDeExclusaoAsync(int usuarioId)
        {
            var previa = new PreviaDeExclusaoDto();
            var campanhas = await _uof.CampanhaRepository.GetCampanhasDoUsuarioParaGestaoAsync(usuarioId);
            foreach (var campanha in campanhas.Where(c => c.MestreId == usuarioId))
            {
                var sucessor = SucessorDaCampanha(campanha, usuarioId);
                if (sucessor != null)
                    previa.Transferidas.Add(new CampanhaTransferidaDto { Campanha = campanha.Nome, NovoDono = sucessor.Usuario?.Nome ?? "outro Mestre" });
                else
                    previa.Encerradas.Add(campanha.Nome);
            }
            return previa;
        }

        public async Task ExcluirContaAsync(int usuarioId, string senha)
        {
            var user = await ConferirSenhaAtualAsync(usuarioId, senha);
            var nome = user.Nome;
            var email = user.Email;

            foreach (var campanha in await _uof.CampanhaRepository.GetCampanhasDoUsuarioParaGestaoAsync(usuarioId))
            {
                if (campanha.MestreId == usuarioId)
                {
                    // Dona: o co-Mestre mais antigo assume; sem co-Mestre, a campanha é encerrada para todos.
                    var sucessor = SucessorDaCampanha(campanha, usuarioId);
                    if (sucessor != null)
                        campanha.MestreId = sucessor.UsuarioId;
                    else
                        campanha.Ativo = false;
                }
                else
                {
                    GarantirMestreDepoisDaSaida(campanha, usuarioId);
                }
            }

            // Sai de todas as campanhas, leva as anotações privadas e a foto; personagens e crônica ficam com o grupo.
            await _uof.UsuarioRepository.DesvincularContaAsync(usuarioId);

            // Dados pessoais: a linha fica só para as referências (personagens, crônica) apontarem para "Conta excluída".
            user.Nome = "Conta excluída";
            user.Email = $"excluida-{usuarioId}-{Guid.NewGuid():N}@conta-excluida.invalid";
            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            user.Ativo = false;
            user.ExcluidoEm = DateTime.UtcNow;
            user.EmailConfirmadoEm = null;
            user.EmailPendente = null;
            user.TokenConfirmacaoHash = null;
            user.TokenConfirmacaoExpiraEm = null;
            user.TokenRedefinicaoHash = null;
            user.TokenRedefinicaoExpiraEm = null;
            user.TokenTrocaEmailHash = null;
            user.TokenTrocaEmailExpiraEm = null;
            user.FotoVersao = 0;
            user.Papel = PlanoService.Jogador;
            user.Plano = PlanosOptions.Gratuito;
            await _uof.CommitAsync();

            // Todas as sessões caem já (inclusive a atual) e os registros de aparelho somem.
            foreach (var familiaId in await _uof.RefreshTokenRepository.RevogarTodasAsync(usuarioId, DateTime.UtcNow))
                _sessoes.Invalidar(familiaId);
            await _uof.RefreshTokenRepository.ApagarTodosAsync(usuarioId);
            _limitador.Zerar(email);

            var corpo = ModeloDeEmail.ContaExcluida(nome, $"{_urlFront}/register", UrlLogo);
            await _email.EnviarAsync(email, "A sua conta foi excluída - Wyvern", corpo.Texto, corpo.Html);
        }

        // Quem assume a campanha: o co-Mestre que está nela há mais tempo. Jogador comum não vira dono sem ter sido promovido.
        private static CampanhaMembro? SucessorDaCampanha(Campanha campanha, int usuarioId) =>
            campanha.Membros?
                .Where(m => m.UsuarioId != usuarioId && m.Papel == PapelCampanha.Mestre && m.Usuario is { Ativo: true })
                .OrderBy(m => m.EntrouEm)
                .FirstOrDefault();

        // Campanha de outra pessoa em que ela era o único Mestre (o dono tinha se rebaixado): sem isso a mesa ficaria sem
        // ninguém para gerenciar. O dono volta a ser Mestre; se o dono já saiu, assume o membro mais antigo.
        private static void GarantirMestreDepoisDaSaida(Campanha campanha, int usuarioId)
        {
            var restantes = campanha.Membros?.Where(m => m.UsuarioId != usuarioId && m.Usuario is { Ativo: true }).ToList() ?? new();
            if (restantes.Count == 0 || restantes.Any(m => m.Papel == PapelCampanha.Mestre)) return;

            var novo = restantes.FirstOrDefault(m => m.UsuarioId == campanha.MestreId) ?? restantes.OrderBy(m => m.EntrouEm).First();
            novo.Papel = PapelCampanha.Mestre;
        }

        // Mesmo bloqueio do login: senão, quem pegasse uma sessão aberta testaria senhas por aqui à vontade.
        private async Task<Usuario> ConferirSenhaAtualAsync(int usuarioId, string senhaAtual)
        {
            var user = await _uof.UsuarioRepository.GetUsuarioAsync(usuarioId)
                       ?? throw new RegraNegocioException("Conta não encontrada.");

            if (_limitador.TempoRestanteDoBloqueio(user.Email) is { } espera)
                throw new LoginBloqueadoException(espera);

            if (!BCrypt.Net.BCrypt.Verify(senhaAtual, user.SenhaHash))
            {
                _limitador.RegistrarFalha(user.Email);
                throw new RegraNegocioException("Senha atual incorreta.");
            }

            _limitador.Zerar(user.Email);
            return user;
        }

        // "joana@exemplo.com" vira "jo***@exemplo.com": o aviso não entrega o endereço novo inteiro a quem lê o antigo.
        private static string MascararEmail(string email)
        {
            var arroba = email.IndexOf('@');
            if (arroba <= 0) return "***";
            var visivel = email[..Math.Min(2, arroba)];
            return $"{visivel}***{email[arroba..]}";
        }

        private static RegraNegocioException SessaoExpirada() => new("Sessão expirada. Entre novamente.");

        private static string HashDoToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();


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

        private async Task<AuthResponseDto> CriarRespostaAsync(Usuario user, Guid familiaId, string? dispositivo)
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
                ExpiraEm = expiraEm,
                Dispositivo = dispositivo
            });
            await _uof.CommitAsync();

            return new AuthResponseDto
            {
                UsuarioId = user.UsuarioId,
                Nome = user.Nome,
                Email = user.Email,
                Papel = user.Papel,
                FotoVersao = user.FotoVersao,
                PodeCriarCampanha = (await _planos.ContaAsync(user.UsuarioId)).PodeCriarCampanha,
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
