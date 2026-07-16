namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemItemResponseDto
    {
        public int PersonagemItemId { get; set; }
        public int PersonagemId { get; set; }
        public string Nome { get; set; }
        public string TipoItem { get; set; }
        public string Raridade { get; set; }
        public string Descricao { get; set; }
    }
}
