namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemDuplicarDto
    {
        /// <summary>Campanha da cópia. Sem valor, a cópia fica na mesma campanha do original.</summary>
        public int? CampanhaId { get; set; }

        /// <summary>Nome da cópia. Sem valor, mantém o nome (em outra campanha) ou ganha " (cópia)" (na mesma).</summary>
        public string? Nome { get; set; }
    }
}
