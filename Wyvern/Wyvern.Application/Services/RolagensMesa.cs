using System.Collections.Concurrent;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Application.Services
{
    /// <summary>O que o cliente informa de uma rolagem. Quem rolou e quando o servidor decide.</summary>
    public class RolagemEntradaDto
    {
        /// <summary>O que foi rolado: "Furtividade", "Ataque: Espada longa", "Iniciativa"...</summary>
        public string Rotulo { get; set; } = string.Empty;
        /// <summary>Ex.: "1d20+5".</summary>
        public string Formula { get; set; } = string.Empty;
        public string? Personagem { get; set; }
        public int Total { get; set; }
        /// <summary>Faces que saíram em cada dado, antes dos modificadores.</summary>
        public int[] Dados { get; set; } = [];
    }

    public record RolagemMesa(Guid Id, int UsuarioId, string Usuario, string? Personagem, string Rotulo, string Formula, int Total, int[] Dados, DateTime Quando);

    public static class RolagemMesaFactory
    {
        public const int MaximoDados = 60;

        /// <summary>Confere os limites do que o cliente mandou e monta a rolagem que a mesa vai ver.</summary>
        public static RolagemMesa Criar(int usuarioId, string usuario, RolagemEntradaDto entrada, DateTime? agora = null)
        {
            var rotulo = (entrada.Rotulo ?? string.Empty).Trim();
            var formula = (entrada.Formula ?? string.Empty).Trim();
            var personagem = string.IsNullOrWhiteSpace(entrada.Personagem) ? null : entrada.Personagem.Trim();
            var dados = entrada.Dados ?? [];

            if (rotulo.Length is 0 or > 60) throw new RegraNegocioException("O rótulo da rolagem deve ter de 1 a 60 caracteres.");
            if (formula.Length is 0 or > 40) throw new RegraNegocioException("A fórmula da rolagem deve ter de 1 a 40 caracteres.");
            if (personagem?.Length > 60) throw new RegraNegocioException("O nome do personagem deve ter até 60 caracteres.");
            if (dados.Length > MaximoDados) throw new RegraNegocioException($"No máximo {MaximoDados} dados por rolagem.");
            if (dados.Any(d => d is < 1 or > 1000)) throw new RegraNegocioException("Resultado de dado inválido.");
            if (Math.Abs(entrada.Total) > 100_000) throw new RegraNegocioException("Total da rolagem fora do limite.");

            return new RolagemMesa(Guid.NewGuid(), usuarioId, usuario, personagem, rotulo, formula, entrada.Total, dados, agora ?? DateTime.UtcNow);
        }
    }

    /// <summary>As últimas rolagens de cada campanha, em memória: quem entra na mesa vê o que aconteceu há pouco.</summary>
    public class HistoricoRolagens
    {
        public const int Limite = 40;

        private readonly ConcurrentDictionary<int, List<RolagemMesa>> _porCampanha = new();

        public void Adicionar(int campanhaId, RolagemMesa rolagem)
        {
            var lista = _porCampanha.GetOrAdd(campanhaId, _ => []);
            lock (lista)
            {
                lista.Add(rolagem);
                if (lista.Count > Limite) lista.RemoveRange(0, lista.Count - Limite);
            }
        }

        /// <summary>Da mais antiga para a mais recente.</summary>
        public IReadOnlyList<RolagemMesa> Recentes(int campanhaId)
        {
            if (!_porCampanha.TryGetValue(campanhaId, out var lista)) return [];
            lock (lista) return lista.ToList();
        }
    }
}
