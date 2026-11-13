namespace Wyvern.Tests;

/// <summary>
/// Pedaços de ficha completos para os testes que criam personagens: desde a WYV-35 a API exige, ao criar,
/// a origem do herói, os atributos e o básico de combate, e o bloco de estatísticas dos NPCs de combate e
/// monstros (ver RegrasDaFicha).
/// </summary>
internal static class Fichas
{
    public static readonly object Atributos = new { forca = 10, destreza = 12, constituicao = 14, inteligencia = 10, sabedoria = 10, carisma = 10 };
    public static readonly object Origem = new { classe = "Guerreiro", nivel = 1, raca = "Humano", antecedente = "Soldado" };
    public static readonly object Combate = new { vidaMaxima = 10, vidaAtual = 10, classeArmadura = 14, deslocamento = "9m" };
    public static readonly object Bloco = new { categoriaUso = "Inimigo", tamanho = "Médio", tipoCriatura = "Humanoide", nivelDesafio = "1" };

    /// <summary>O bloco de NPC só para quem não é herói.</summary>
    public static object? BlocoSe(int tipoId) => tipoId == 1 ? null : Bloco;

    /// <summary>Um herói nível 1 completo, para os testes que só precisam de "um personagem".</summary>
    public static object Heroi(string nome, int campanhaId) =>
        new { nome, campanhaId, tipoId = 1, personagemPlayer = Origem, atributo = Atributos, personagemCombate = Combate };
}
