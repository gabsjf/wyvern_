using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Personagem
{
    /// <summary>
    /// Resultado de um descanso já calculado: pelo front (que rolou os dados) ou digitado pelo jogador
    /// depois de rolar dados físicos. O servidor confere os limites e grava.
    /// </summary>
    public class PersonagemDescansoDto
    {
        /// <summary>"Curto" ou "Longo".</summary>
        [Required]
        public string Tipo { get; set; } = string.Empty;

        public int VidaAtual { get; set; }
        public int VidaTemporaria { get; set; }
        public int DadoVidaGasto { get; set; }
        public int DeathSaveSucessos { get; set; }
        public int DeathSaveFalhas { get; set; }

        /// <summary>Devolve todos os espaços de magia (descanso longo; no curto serve para o Bruxo, por exemplo).</summary>
        public bool RestaurarEspacosMagia { get; set; }
    }
}
