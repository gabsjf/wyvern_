namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemNpcDto
    {
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
