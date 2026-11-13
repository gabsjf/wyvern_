using Wyvern.Application.DTOs.Atributo;
using Wyvern.Application.DTOs.Personagem;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// O que cada tipo de personagem precisa ter para ser criado, pelas regras do D&amp;D 2024:
    /// <list type="bullet">
    /// <item>Herói: a origem (classe, espécie, antecedente), o nível, os seis atributos e o básico de combate
    /// (PV, CA, deslocamento); subclasse a partir do nível 3.</item>
    /// <item>NPC narrativo: só o nome. Não entra em combate.</item>
    /// <item>NPC de combate, vilão e monstro: o bloco de estatísticas (tamanho, tipo de criatura, CA, PV,
    /// deslocamento, atributos e nível de desafio).</item>
    /// </list>
    /// Vale só ao criar: fichas antigas continuam editáveis, e a importação de PDF pode trazer ficha incompleta.
    /// </summary>
    public static class RegrasDaFicha
    {
        public const int Heroi = 1, Npc = 2, Monstro = 3;
        public const string Narrativo = "Narrativo", Inimigo = "Inimigo", Vilao = "Vilao";

        /// <summary>XP por nível de desafio (tabela do Livro dos Monstros).</summary>
        public static readonly IReadOnlyDictionary<string, int> XpPorNd = new Dictionary<string, int>
        {
            ["0"] = 10, ["1/8"] = 25, ["1/4"] = 50, ["1/2"] = 100,
            ["1"] = 200, ["2"] = 450, ["3"] = 700, ["4"] = 1100, ["5"] = 1800, ["6"] = 2300, ["7"] = 2900, ["8"] = 3900,
            ["9"] = 5000, ["10"] = 5900, ["11"] = 7200, ["12"] = 8400, ["13"] = 10000, ["14"] = 11500, ["15"] = 13000,
            ["16"] = 15000, ["17"] = 18000, ["18"] = 20000, ["19"] = 22000, ["20"] = 25000, ["21"] = 33000, ["22"] = 41000,
            ["23"] = 50000, ["24"] = 62000, ["25"] = 75000, ["26"] = 90000, ["27"] = 105000, ["28"] = 120000,
            ["29"] = 135000, ["30"] = 155000,
        };

        /// <summary>Entra em combate com bloco de estatísticas: monstro, ou NPC de combate ou vilão.</summary>
        public static bool TemBlocoDeCombate(int tipoId, string? categoria) =>
            tipoId == Monstro || (tipoId == Npc && (categoria == Inimigo || categoria == Vilao));

        /// <summary>O que falta para criar o personagem (vazio = pode criar). Mensagens prontas para mostrar ao usuário.</summary>
        public static List<string> FaltasParaCriar(PersonagemCreateDto dto, int tipoId)
        {
            var faltas = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.Nome)) faltas.Add("Dê um nome ao personagem.");

            if (tipoId == Heroi)
            {
                var p = dto.PersonagemPlayer;
                if (string.IsNullOrWhiteSpace(p?.Classe)) faltas.Add("Escolha a classe.");
                if (string.IsNullOrWhiteSpace(p?.Raca)) faltas.Add("Escolha a espécie.");
                if (string.IsNullOrWhiteSpace(p?.Antecedente)) faltas.Add("Escolha o antecedente.");
                if (p == null || p.Nivel is < 1 or > 20) faltas.Add("O nível vai de 1 a 20.");
                else if (p.Nivel >= 3 && string.IsNullOrWhiteSpace(p.Subclasse)) faltas.Add("A partir do nível 3, escolha a subclasse.");
                FaltasDeAtributos(dto.Atributo, faltas);
                FaltasDeCombate(dto.PersonagemCombate, faltas);
                return faltas;
            }

            var npc = dto.PersonagemNpc;
            if (!TemBlocoDeCombate(tipoId, tipoId == Monstro ? Inimigo : npc?.CategoriaUso)) return faltas; // NPC narrativo: só o nome

            if (string.IsNullOrWhiteSpace(npc?.Tamanho)) faltas.Add("Informe o tamanho.");
            if (string.IsNullOrWhiteSpace(npc?.TipoCriatura)) faltas.Add("Informe o tipo de criatura.");
            if (npc == null || !XpPorNd.ContainsKey((npc.NivelDesafio ?? "").Trim())) faltas.Add("Escolha o nível de desafio (ND).");
            FaltasDeAtributos(dto.Atributo, faltas);
            FaltasDeCombate(dto.PersonagemCombate, faltas);
            return faltas;
        }

        /// <summary>XP do ND, para quando o Mestre não informou outro valor.</summary>
        public static int? XpDoNd(string? nd) => XpPorNd.TryGetValue((nd ?? "").Trim(), out var xp) ? xp : null;

        private static void FaltasDeAtributos(CreateAtributoDto? a, List<string> faltas)
        {
            if (a == null) { faltas.Add("Preencha os seis atributos."); return; }
            int[] valores = { a.Forca, a.Destreza, a.Constituicao, a.Inteligencia, a.Sabedoria, a.Carisma };
            if (valores.Any(v => v is < 1 or > 30)) faltas.Add("Cada atributo vai de 1 a 30.");
        }

        private static void FaltasDeCombate(PersonagemCombateUpdateDto? c, List<string> faltas)
        {
            if (c == null || c.VidaMaxima < 1) faltas.Add("Informe os pontos de vida máximos.");
            if (c == null || c.ClasseArmadura < 1) faltas.Add("Informe a classe de armadura (CA).");
            if (string.IsNullOrWhiteSpace(c?.Deslocamento)) faltas.Add("Informe o deslocamento.");
        }
    }
}
