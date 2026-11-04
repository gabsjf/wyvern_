using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>O texto que a crônica registra quando um combate termina: quem caiu e quem ficou de pé.</summary>
    public static class ResumoDeCombate
    {
        public static (string Titulo, string Descricao) Criar(Combate combate)
        {
            var participantes = combate.Participantes ?? [];
            var rodadas = Math.Max(1, combate.RodadaAtual);

            static string Nome(CombateParticipante p) => string.IsNullOrWhiteSpace(p.NomeNPC) ? (p.Personagem?.Nome ?? "?") : p.NomeNPC!;
            static bool Caido(CombateParticipante p) => p.VidaAtual <= 0;

            var inimigosDerrotados = participantes.Where(p => p.IsInimigo && Caido(p)).Select(Nome).ToList();
            var inimigosEmPe = participantes.Where(p => p.IsInimigo && !Caido(p)).Select(Nome).ToList();
            var herois = participantes.Where(p => !p.IsInimigo).ToList();
            var heroisCaidos = herois.Where(Caido).Select(Nome).ToList();
            var heroisEmPe = herois.Where(p => !Caido(p)).Select(Nome).ToList();

            var linhas = new List<string>();
            if (inimigosDerrotados.Count > 0) linhas.Add($"**Derrotados:** {string.Join(", ", inimigosDerrotados)}");
            if (inimigosEmPe.Count > 0) linhas.Add($"**Inimigos que restaram:** {string.Join(", ", inimigosEmPe)}");
            if (heroisCaidos.Count > 0) linhas.Add($"**Heróis caídos:** {string.Join(", ", heroisCaidos)}");
            if (heroisEmPe.Count > 0) linhas.Add($"**Heróis de pé:** {string.Join(", ", heroisEmPe)}");

            var titulo = inimigosDerrotados.Count > 0
                ? $"Combate: {inimigosDerrotados.Count} {(inimigosDerrotados.Count == 1 ? "inimigo derrotado" : "inimigos derrotados")} em {rodadas} {(rodadas == 1 ? "rodada" : "rodadas")}"
                : $"Combate encerrado em {rodadas} {(rodadas == 1 ? "rodada" : "rodadas")}";

            return (titulo, string.Join("\n\n", linhas));
        }
    }
}
