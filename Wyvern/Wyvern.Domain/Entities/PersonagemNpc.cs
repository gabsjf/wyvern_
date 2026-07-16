using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    public class PersonagemNpc
    {
        [Key]
        [ForeignKey("Personagem")]
        public int PersonagemId { get; set; }
        public Personagem Personagem { get; set; }

        public string? CategoriaUso { get; set; }
        public string? Tamanho { get; set; }
        public string? TipoCriatura { get; set; }
        public string? Tendencia { get; set; }

        public string? FormulaDadoVida { get; set; }

        public string? Vulnerabilidades { get; set; }
        public string? Resistencias { get; set; }
        public string? ImunidadesDano { get; set; }
        public string? ImunidadesCondicao { get; set; }
        public string? Sentidos { get; set; }
        
        public string? NivelDesafio { get; set; }
        public int XpConcedido { get; set; }

        public string? VinculosIdeais { get; set; }
        public string? SegredosFaccoes { get; set; }
        public string? AnotacoesLivres { get; set; }
    }
}
