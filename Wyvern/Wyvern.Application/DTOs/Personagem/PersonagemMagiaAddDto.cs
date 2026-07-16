using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemMagiaAddDto
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; }
        public int Nivel { get; set; }
        public string Escola { get; set; }
        public bool Verbal { get; set; }
        public bool Somatico { get; set; }
        public bool Material { get; set; }
        public string Descricao { get; set; }
    }
}
