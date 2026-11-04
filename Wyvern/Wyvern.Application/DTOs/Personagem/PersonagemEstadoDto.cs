namespace Wyvern.Application.DTOs.Personagem
{
    /// <summary>
    /// Mudança parcial no estado de jogo da ficha (o que muda durante a sessão). Só o que vier preenchido é alterado.
    /// </summary>
    public class PersonagemEstadoDto
    {
        public int? VidaAtual { get; set; }
        public int? VidaTemporaria { get; set; }
        public bool? InspiracaoHeroica { get; set; }
        public int? DeathSaveSucessos { get; set; }
        public int? DeathSaveFalhas { get; set; }

        /// <summary>Condições ativas, separadas por vírgula (ex.: "Envenenado, Caído"). Vazio limpa todas.</summary>
        public string? Condicoes { get; set; }

        /// <summary>Espaços de magia não gastos, do 1º ao 9º nível (9 posições).</summary>
        public int[]? EspacosAtuais { get; set; }
    }
}
