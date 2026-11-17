using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Um herói em combate vive em dois lugares: na ficha (<see cref="PersonagemCombate"/>) e no combatente do rastreador
    /// (<see cref="CombateParticipante"/>). Os dois mostram o mesmo PV, PV máximo, CA, condições e salvaguardas de morte: o
    /// que mudar num lado vai para o outro. A iniciativa não entra (no combate é o valor rolado; na ficha, o bônus), e o
    /// PV temporário só existe na ficha, então o dano que vem do combate passa por ele antes.
    /// Monstros e NPCs ficam de fora: o mesmo monstro pode entrar várias vezes no combate, e a ficha dele é um molde.
    /// </summary>
    public interface ISincronizacaoDeCombate
    {
        /// <summary>
        /// Leva a ficha do herói a todos os combates ativos em que ele está (sem gravar: o Commit é de quem chamou).
        /// Devolve os combates que mudaram, para avisar as mesas.
        /// </summary>
        Task<IReadOnlyList<Combate>> LevarFichaAosCombatesAsync(Personagem personagem);
    }

    public class SincronizacaoDeCombate : ISincronizacaoDeCombate
    {
        public const int TipoHeroi = 1;

        private readonly IUnitOfWork _uof;

        public SincronizacaoDeCombate(IUnitOfWork uof) => _uof = uof;

        public static bool Sincroniza(Personagem? personagem) =>
            personagem is { TipoId: TipoHeroi, PersonagemCombate: not null };

        public async Task<IReadOnlyList<Combate>> LevarFichaAosCombatesAsync(Personagem personagem)
        {
            if (!Sincroniza(personagem)) return [];

            var mudaram = new Dictionary<int, Combate>();
            foreach (var participante in await _uof.CombateRepository.GetParticipantesAtivosDoPersonagemAsync(personagem.PersonagemId))
            {
                if (!CopiarDaFicha(personagem.PersonagemCombate!, participante)) continue;
                await _uof.CombateRepository.UpdateParticipanteAsync(participante);
                mudaram[participante.CombateId] = participante.Combate!;
            }
            return mudaram.Values.ToList();
        }

        /// <summary>Ficha → combatente. Devolve se algo mudou.</summary>
        public static bool CopiarDaFicha(PersonagemCombate ficha, CombateParticipante participante)
        {
            var antes = (participante.VidaAtual, participante.VidaMaxima, participante.ClasseArmadura, participante.Condicoes,
                participante.SucessosMorte, participante.FalhasMorte);

            participante.VidaAtual = ficha.VidaAtual;
            participante.VidaMaxima = ficha.VidaMaxima;
            participante.ClasseArmadura = ficha.ClasseArmadura;
            participante.Condicoes = ficha.Condicoes ?? string.Empty;
            participante.SucessosMorte = ficha.DeathSaveSucessos;
            participante.FalhasMorte = ficha.DeathSaveFalhas;

            return antes != (participante.VidaAtual, participante.VidaMaxima, participante.ClasseArmadura, participante.Condicoes,
                participante.SucessosMorte, participante.FalhasMorte);
        }

        /// <summary>
        /// Combatente → ficha, depois de o rastreador mudar o combatente. O rastreador manda o PV final; a diferença para
        /// <paramref name="vidaAnterior"/> é o dano (ou a cura) e, no dano, o PV temporário da ficha absorve primeiro.
        /// No fim, o combatente fica com o PV que a ficha calculou.
        /// </summary>
        public static void LevarParaFicha(CombateParticipante participante, int vidaAnterior, PersonagemCombate ficha)
        {
            var diferenca = participante.VidaAtual - vidaAnterior;
            if (diferenca < 0)
            {
                var dano = -diferenca;
                var absorvido = Math.Min(Math.Max(ficha.VidaTemporaria, 0), dano);
                ficha.VidaTemporaria -= absorvido;
                ficha.VidaAtual -= dano - absorvido;
            }
            else
            {
                ficha.VidaAtual += diferenca;
            }
            ficha.VidaAtual = Math.Clamp(ficha.VidaAtual, 0, Math.Max(ficha.VidaMaxima, 0));

            ficha.Condicoes = string.IsNullOrWhiteSpace(participante.Condicoes) ? null : participante.Condicoes.Trim();
            ficha.DeathSaveSucessos = Math.Clamp(participante.SucessosMorte, 0, 3);
            ficha.DeathSaveFalhas = Math.Clamp(participante.FalhasMorte, 0, 3);

            CopiarDaFicha(ficha, participante);
        }
    }
}
