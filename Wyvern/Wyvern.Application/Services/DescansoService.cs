using System.Text.RegularExpressions;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Aplica o resultado de um descanso (curto ou longo) na ficha. Os valores vêm calculados (pelo sistema ou
    /// digitados pelo jogador), então aqui só se confere que cabem nos limites da ficha.
    /// </summary>
    public static class DescansoService
    {
        private static readonly Regex DadoDeVida = new(@"^\s*(\d*)\s*d\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Quantos dados de vida o personagem tem no total: "5d10" vale 5; sem número ("d10"), o nível.</summary>
        public static int TotalDadosVida(Personagem personagem)
        {
            var nivel = Math.Max(0, personagem.PersonagemPlayer?.Nivel ?? 0);
            var texto = personagem.PersonagemCombate?.DadoVidaMaximo;
            if (string.IsNullOrWhiteSpace(texto)) return nivel;

            var m = DadoDeVida.Match(texto);
            if (!m.Success) return nivel;
            return int.TryParse(m.Groups[1].Value, out var quantidade) ? quantidade : nivel;
        }

        public static void Aplicar(Personagem personagem, PersonagemDescansoDto dto)
        {
            if (!string.Equals(dto.Tipo, "Curto", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(dto.Tipo, "Longo", StringComparison.OrdinalIgnoreCase))
                throw new RegraNegocioException("O tipo do descanso deve ser \"Curto\" ou \"Longo\".");

            var combate = personagem.PersonagemCombate
                ?? throw new RegraNegocioException("Este personagem ainda não tem pontos de vida definidos na ficha.");

            if (dto.VidaAtual < 0 || dto.VidaAtual > combate.VidaMaxima)
                throw new RegraNegocioException($"Os pontos de vida devem ficar entre 0 e {combate.VidaMaxima}.");
            if (dto.VidaTemporaria < 0)
                throw new RegraNegocioException("Os pontos de vida temporários não podem ser negativos.");

            // Dados de vida gastos: de 0 ao total (se a ficha já estiver acima do total, não obriga a corrigir isso agora)
            var limiteDados = Math.Max(TotalDadosVida(personagem), combate.DadoVidaGasto);
            if (dto.DadoVidaGasto < 0 || dto.DadoVidaGasto > limiteDados)
                throw new RegraNegocioException($"Os dados de vida gastos devem ficar entre 0 e {limiteDados}.");

            if (dto.DeathSaveSucessos is < 0 or > 3 || dto.DeathSaveFalhas is < 0 or > 3)
                throw new RegraNegocioException("As salvaguardas de morte vão de 0 a 3.");

            combate.VidaAtual = dto.VidaAtual;
            combate.VidaTemporaria = dto.VidaTemporaria;
            combate.DadoVidaGasto = dto.DadoVidaGasto;
            combate.DeathSaveSucessos = dto.DeathSaveSucessos;
            combate.DeathSaveFalhas = dto.DeathSaveFalhas;

            if (dto.RestaurarEspacosMagia && personagem.PersonagemConjuracao is { } c)
            {
                c.SlotsNivel1Atual = c.SlotsNivel1Max;
                c.SlotsNivel2Atual = c.SlotsNivel2Max;
                c.SlotsNivel3Atual = c.SlotsNivel3Max;
                c.SlotsNivel4Atual = c.SlotsNivel4Max;
                c.SlotsNivel5Atual = c.SlotsNivel5Max;
                c.SlotsNivel6Atual = c.SlotsNivel6Max;
                c.SlotsNivel7Atual = c.SlotsNivel7Max;
                c.SlotsNivel8Atual = c.SlotsNivel8Max;
                c.SlotsNivel9Atual = c.SlotsNivel9Max;
            }
        }
    }
}
