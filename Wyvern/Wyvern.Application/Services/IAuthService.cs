using System.Security.Claims;
using System.Threading.Tasks;
using Wyvern.Application.DTOs.Auth;
using Wyvern.Application.DTOs.Usuario;

namespace Wyvern.Application.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Resposta idêntica com o e-mail novo ou já cadastrado: ninguém descobre quem tem conta. Não abre sessão; o dono do
        /// e-mail recebe uma mensagem (confirmação, se é novo; aviso, se já existia).
        /// </summary>
        Task RegisterAsync(RegisterDto dto);
        Task ConfirmarEmailAsync(string token);

        /// <summary>Sempre termina sem erro, exista a conta ou não; o link vai por e-mail.</summary>
        Task SolicitarRedefinicaoDeSenhaAsync(string email);
        Task RedefinirSenhaAsync(string token, string novaSenha);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, string? dispositivo = null);

        /// <summary>Rotação: devolve access + refresh novos; reuso de um refresh já trocado revoga a família.</summary>
        Task<AuthResponseDto> RefreshAsync(string? refreshToken);
        Task EncerrarSessaoAsync(string? refreshToken);

        /// <summary>Token curto, só para abrir a conexão de um hub; não serve na API REST.</summary>
        string GerarTicketHub(ClaimsPrincipal usuario);

        // ---------- tela "Meu perfil" ----------

        /// <summary>Confere a senha atual (com o mesmo bloqueio do login), troca e derruba as outras sessões; a atual continua.</summary>
        Task TrocarSenhaAsync(int usuarioId, Guid? sessaoAtual, string senhaAtual, string novaSenha);

        /// <summary>Confere a senha e manda o link ao e-mail novo; a troca só acontece no clique. Não revela se o e-mail já tem dono.</summary>
        Task SolicitarTrocaDeEmailAsync(int usuarioId, string novoEmail, string senhaAtual);
        Task ConfirmarTrocaDeEmailAsync(string token);

        Task<List<SessaoAtivaDto>> ListarSessoesAsync(int usuarioId, Guid? sessaoAtual);

        /// <summary>Falso se a sessão não existe ou é de outra pessoa.</summary>
        Task<bool> EncerrarSessaoDoUsuarioAsync(int usuarioId, Guid sessaoAtual, Guid sessaoId);
        Task EncerrarOutrasSessoesAsync(int usuarioId, Guid sessaoAtual);

        // ---------- exclusão de conta ----------

        /// <summary>Quais campanhas da pessoa passam a um co-Mestre e quais são encerradas.</summary>
        Task<PreviaDeExclusaoDto> PreviaDeExclusaoAsync(int usuarioId);

        /// <summary>
        /// Confere a senha e exclui na hora: campanhas passam ao co-Mestre ou são encerradas, anotações privadas somem, os
        /// personagens ficam nas mesas, os dados pessoais são apagados (a linha vira "Conta excluída") e todas as sessões caem.
        /// O e-mail fica livre para um novo cadastro e recebe um aviso.
        /// </summary>
        Task ExcluirContaAsync(int usuarioId, string senha);
    }
}
