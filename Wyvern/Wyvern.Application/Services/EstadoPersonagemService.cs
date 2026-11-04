using Wyvern.Application.DTOs.Personagem;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Exceptions;

namespace Wyvern.Application.Services
{
    /// <summary>Aplica mudanças parciais de estado de jogo (PV, condições, espaços de magia...) conferindo os limites da ficha.</summary>
    public static class EstadoPersonagemService
    {
        public const int MaximoCondicoes = 300;

        public static void Aplicar(Personagem personagem, PersonagemEstadoDto dto)
        {
            var combate = personagem.PersonagemCombate;

            var mexeNoCombate = dto.VidaAtual != null || dto.VidaTemporaria != null || dto.InspiracaoHeroica != null
                || dto.DeathSaveSucessos != null || dto.DeathSaveFalhas != null || dto.Condicoes != null;
            if (mexeNoCombate && combate == null)
                throw new RegraNegocioException("Este personagem ainda não tem pontos de vida definidos na ficha.");

            if (dto.VidaAtual is { } vida)
            {
                if (vida < 0 || vida > combate!.VidaMaxima)
                    throw new RegraNegocioException($"Os pontos de vida devem ficar entre 0 e {combate.VidaMaxima}.");
                combate.VidaAtual = vida;
            }

            if (dto.VidaTemporaria is { } temporarios)
            {
                if (temporarios < 0) throw new RegraNegocioException("Os pontos de vida temporários não podem ser negativos.");
                combate!.VidaTemporaria = temporarios;
            }

            if (dto.InspiracaoHeroica is { } inspiracao) combate!.InspiracaoHeroica = inspiracao;

            if (dto.DeathSaveSucessos is { } sucessos)
            {
                if (sucessos is < 0 or > 3) throw new RegraNegocioException("As salvaguardas de morte vão de 0 a 3.");
                combate!.DeathSaveSucessos = sucessos;
            }

            if (dto.DeathSaveFalhas is { } falhas)
            {
                if (falhas is < 0 or > 3) throw new RegraNegocioException("As salvaguardas de morte vão de 0 a 3.");
                combate!.DeathSaveFalhas = falhas;
            }

            if (dto.Condicoes != null)
            {
                if (dto.Condicoes.Length > MaximoCondicoes)
                    throw new RegraNegocioException($"As condições aceitam até {MaximoCondicoes} caracteres.");
                combate!.Condicoes = dto.Condicoes.Trim();
            }

            if (dto.EspacosAtuais != null) AplicarEspacos(personagem, dto.EspacosAtuais);
        }

        /// <summary>
        /// Personagem recém-criado nasce com os espaços de magia disponíveis: o formulário não pergunta quantos já foram
        /// gastos, então "atual = 0" aqui só quer dizer "não informado", e não "tudo gasto".
        /// </summary>
        public static void EncherEspacosDeMagiaNovos(PersonagemConjuracao? c)
        {
            if (c == null) return;
            if (c.SlotsNivel1Atual == 0) c.SlotsNivel1Atual = c.SlotsNivel1Max;
            if (c.SlotsNivel2Atual == 0) c.SlotsNivel2Atual = c.SlotsNivel2Max;
            if (c.SlotsNivel3Atual == 0) c.SlotsNivel3Atual = c.SlotsNivel3Max;
            if (c.SlotsNivel4Atual == 0) c.SlotsNivel4Atual = c.SlotsNivel4Max;
            if (c.SlotsNivel5Atual == 0) c.SlotsNivel5Atual = c.SlotsNivel5Max;
            if (c.SlotsNivel6Atual == 0) c.SlotsNivel6Atual = c.SlotsNivel6Max;
            if (c.SlotsNivel7Atual == 0) c.SlotsNivel7Atual = c.SlotsNivel7Max;
            if (c.SlotsNivel8Atual == 0) c.SlotsNivel8Atual = c.SlotsNivel8Max;
            if (c.SlotsNivel9Atual == 0) c.SlotsNivel9Atual = c.SlotsNivel9Max;
        }

        private static void AplicarEspacos(Personagem personagem, int[] atuais)
        {
            var c = personagem.PersonagemConjuracao
                ?? throw new RegraNegocioException("Este personagem não tem espaços de magia.");
            if (atuais.Length != 9) throw new RegraNegocioException("Informe os espaços de magia dos 9 níveis.");

            int[] maximos = [c.SlotsNivel1Max, c.SlotsNivel2Max, c.SlotsNivel3Max, c.SlotsNivel4Max, c.SlotsNivel5Max,
                             c.SlotsNivel6Max, c.SlotsNivel7Max, c.SlotsNivel8Max, c.SlotsNivel9Max];
            for (var i = 0; i < 9; i++)
                if (atuais[i] < 0 || atuais[i] > maximos[i])
                    throw new RegraNegocioException($"Os espaços de {i + 1}º nível devem ficar entre 0 e {maximos[i]}.");

            c.SlotsNivel1Atual = atuais[0];
            c.SlotsNivel2Atual = atuais[1];
            c.SlotsNivel3Atual = atuais[2];
            c.SlotsNivel4Atual = atuais[3];
            c.SlotsNivel5Atual = atuais[4];
            c.SlotsNivel6Atual = atuais[5];
            c.SlotsNivel7Atual = atuais[6];
            c.SlotsNivel8Atual = atuais[7];
            c.SlotsNivel9Atual = atuais[8];
        }
    }
}
