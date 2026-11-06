using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Freio de força bruta por conta, somado ao limite por IP. Depois de N senhas erradas o login daquele e-mail fica
    /// bloqueado por um tempo que dobra a cada nova falha, até um teto; o bloqueio é sempre temporário.
    /// A chave é o e-mail digitado, exista a conta ou não, para o comportamento ser idêntico e não revelar quem tem conta.
    /// </summary>
    public interface ILimitadorDeLogin
    {
        /// <summary>Quanto falta para o login deste e-mail voltar a ser aceito; nulo se não está bloqueado.</summary>
        TimeSpan? TempoRestanteDoBloqueio(string email);

        /// <summary>Conta uma senha errada e, passado o limite, abre (ou alonga) o bloqueio.</summary>
        void RegistrarFalha(string email);

        /// <summary>Esquece as falhas: senha certa, ou senha redefinida pelo link do e-mail.</summary>
        void Zerar(string email);
    }

    /// <summary>
    /// Configuração (seção Login): TentativasAntesDoBloqueio (5), BloqueioInicialSegundos (30), BloqueioMaximoSegundos (900).
    /// Com os padrões o bloqueio vai de 30 s a 1, 2, 4, 8 e 15 min (teto). O contador some sozinho depois de 30 min sem falhas.
    /// O estado fica em memória: um reinício da API (único processo) o zera, o que só alivia o dono da conta.
    /// </summary>
    public class LimitadorDeLogin : ILimitadorDeLogin
    {
        private static readonly TimeSpan JanelaDeEsquecimento = TimeSpan.FromMinutes(30);

        private readonly IMemoryCache _cache;
        private readonly TimeProvider _relogio;
        private readonly int _tentativas;
        private readonly TimeSpan _bloqueioInicial;
        private readonly TimeSpan _bloqueioMaximo;

        public LimitadorDeLogin(IMemoryCache cache, TimeProvider relogio, IConfiguration configuration)
        {
            _cache = cache;
            _relogio = relogio;
            _tentativas = Math.Max(1, configuration.GetValue("Login:TentativasAntesDoBloqueio", 5));
            _bloqueioInicial = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Login:BloqueioInicialSegundos", 30)));
            _bloqueioMaximo = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Login:BloqueioMaximoSegundos", 900)));
        }

        private sealed class Estado
        {
            public int Falhas;
            public DateTimeOffset UltimaFalha;
            public DateTimeOffset? BloqueadoAte;
        }

        public TimeSpan? TempoRestanteDoBloqueio(string email)
        {
            if (!_cache.TryGetValue(Chave(email), out Estado? estado) || estado?.BloqueadoAte is not { } ate)
                return null;

            var restante = ate - _relogio.GetUtcNow();
            return restante > TimeSpan.Zero ? restante : null;
        }

        public void RegistrarFalha(string email)
        {
            var agora = _relogio.GetUtcNow();
            var chave = Chave(email);
            _cache.TryGetValue(chave, out Estado? estado);

            // Muito tempo sem falhar: recomeça do zero (o dono que errou há uma hora não herda o histórico).
            if (estado == null || agora - estado.UltimaFalha > JanelaDeEsquecimento)
                estado = new Estado();

            estado.Falhas++;
            estado.UltimaFalha = agora;
            if (estado.Falhas >= _tentativas)
                estado.BloqueadoAte = agora + AtrasoDaFalha(estado.Falhas - _tentativas);

            // Guarda enquanto o bloqueio e a janela de esquecimento ainda importarem.
            _cache.Set(chave, estado, _bloqueioMaximo + JanelaDeEsquecimento);
        }

        public void Zerar(string email) => _cache.Remove(Chave(email));

        // Inicial * 2^n, travado no teto (o limite do expoente evita estouro em contas muito atacadas).
        private TimeSpan AtrasoDaFalha(int excedentes)
        {
            var atraso = _bloqueioInicial * Math.Pow(2, Math.Min(excedentes, 20));
            return atraso > _bloqueioMaximo ? _bloqueioMaximo : atraso;
        }

        private static string Chave(string email) => $"login-falhas:{email.Trim().ToLowerInvariant()}";
    }
}
