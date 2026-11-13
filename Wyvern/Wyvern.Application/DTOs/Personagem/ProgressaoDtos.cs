namespace Wyvern.Application.DTOs.Personagem
{
    /// <summary>O Mestre ajusta o XP e/ou libera um nível por marco.</summary>
    public class ProgressaoDto
    {
        public int? Xp { get; set; }
        /// <summary>Nível liberado por marco (0 tira a liberação).</summary>
        public int? NivelLiberado { get; set; }
    }

    public class RecursoComparadoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Antes { get; set; }
        public string? Depois { get; set; }
    }

    public class ValorComparadoDto
    {
        public int Antes { get; set; }
        public int Depois { get; set; }
    }

    /// <summary>O que muda no próximo nível, calculado pelo servidor a partir das regras do D&D 2024.</summary>
    public class ProximoNivelDto
    {
        /// <summary>A classe da ficha é uma das 12 do livro (senão só PV, proficiência e dados de vida são calculados).</summary>
        public bool ClasseReconhecida { get; set; }
        public string Classe { get; set; } = string.Empty;
        public int NivelAtual { get; set; }
        public int ProximoNivel { get; set; }
        public int NivelDisponivel { get; set; }
        public bool PodeSubir { get; set; }
        /// <summary>Por que não pode subir (nível máximo, nada liberado, ficha sem PV...).</summary>
        public string? Motivo { get; set; }

        public int Xp { get; set; }
        /// <summary>XP do próximo nível (nulo no 20).</summary>
        public int? XpProximoNivel { get; set; }
        public int NivelLiberado { get; set; }

        public int DadoVida { get; set; }
        public int PvFixo { get; set; }
        public int ModConstituicao { get; set; }
        public int VidaMaxima { get; set; }

        public ValorComparadoDto Proficiencia { get; set; } = new();
        public List<string> Caracteristicas { get; set; } = new();
        public List<RecursoComparadoDto> Recursos { get; set; } = new();
        /// <summary>Espaços de magia máximos do 1º ao 9º círculo, antes e depois (nulo se a classe não conjura).</summary>
        public int[]? EspacosAntes { get; set; }
        public int[]? EspacosDepois { get; set; }
        public ValorComparadoDto? Truques { get; set; }
        public ValorComparadoDto? MagiasPreparadas { get; set; }

        public bool EscolheSubclasse { get; set; }
        public bool MelhoriaDeAtributo { get; set; }
        public bool DadivaEpica { get; set; }
        public Dictionary<string, int> Atributos { get; set; } = new();
    }

    /// <summary>As escolhas do jogador ao subir de nível.</summary>
    public class SubirNivelDto
    {
        /// <summary>"Fixo" (média do dado) ou "Rolado".</summary>
        public string ModoPv { get; set; } = "Fixo";
        /// <summary>Resultado do dado de vida quando rolado (sem o modificador de Constituição).</summary>
        public int? PvRolado { get; set; }
        public string? Subclasse { get; set; }
        /// <summary>Nos níveis de Melhoria de Atributo e na Dádiva Épica.</summary>
        public MelhoriaEscolhidaDto? Melhoria { get; set; }
    }

    public class MelhoriaEscolhidaDto
    {
        /// <summary>"Atributos" (o talento Aprimoramento de Atributo: +2 em um ou +1 em dois) ou "Talento" (outro talento).</summary>
        public string Tipo { get; set; } = "Atributos";
        /// <summary>Nome do talento escolhido (obrigatório em "Talento" e na Dádiva Épica).</summary>
        public string? Talento { get; set; }
        /// <summary>Aumentos por atributo: chaves forca, destreza, constituicao, inteligencia, sabedoria, carisma.</summary>
        public Dictionary<string, int> Aumentos { get; set; } = new();
    }

    public class ResultadoSubidaDto
    {
        public PersonagemResponseDto Personagem { get; set; } = new();
        public int NivelNovo { get; set; }
        public int PvGanhos { get; set; }
        public string Resumo { get; set; } = string.Empty;
    }
}
