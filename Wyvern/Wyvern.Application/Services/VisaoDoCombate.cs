using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// O que o jogador sabe dos inimigos no combate: só o estado da vida (como a tela mostra: saudável, machucado, muito
    /// machucado, quase morto, abatido), nunca o PV e a CA de verdade. O servidor manda um valor que representa a faixa,
    /// numa escala de 100, para a barra e o texto da tela continuarem funcionando sem os números saírem para a rede.
    /// </summary>
    public static class VisaoDoCombate
    {
        public const int Escala = 100;

        /// <summary>Mesmas faixas de obterStatusVida no rastreador.</summary>
        public static int FaixaDaVida(int atual, int maxima)
        {
            if (atual <= 0) return 0;
            if (maxima <= 0) return Escala;
            var pct = (double)atual / maxima;
            return pct > 0.75 ? Escala : pct > 0.35 ? 60 : pct > 0.15 ? 25 : 10;
        }

        /// <summary>A cópia do combatente que vai para um jogador: inimigos sem PV nem CA de verdade.</summary>
        public static CombateParticipante ParaJogador(CombateParticipante p) => !p.IsInimigo ? p : new CombateParticipante
        {
            ParticipanteId = p.ParticipanteId,
            CombateId = p.CombateId,
            PersonagemId = p.PersonagemId,
            NomeNPC = p.NomeNPC,
            Iniciativa = p.Iniciativa,
            VidaAtual = FaixaDaVida(p.VidaAtual, p.VidaMaxima),
            VidaMaxima = Escala,
            ClasseArmadura = 0,
            IsInimigo = true,
            Condicoes = p.Condicoes,
            SucessosMorte = p.SucessosMorte,
            FalhasMorte = p.FalhasMorte,
        };
    }
}
