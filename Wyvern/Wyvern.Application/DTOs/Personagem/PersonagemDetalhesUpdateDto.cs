namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemDetalhesUpdateDto
    {
        public string? Aparencia { get; set; }
        public string? HistoriaPersonalidade { get; set; }
        public string? TracosEspecie { get; set; }
        public string? Talentos { get; set; }
        public string? CaracteristicasClasse { get; set; }
        public string? ProficienciaArmas { get; set; }
        public string? ProficienciaFerramentas { get; set; }
        public string? Idiomas { get; set; }

        public bool ProficienciaArmaduraLeve { get; set; }
        public bool ProficienciaArmaduraMedia { get; set; }
        public bool ProficienciaArmaduraPesada { get; set; }
        public bool ProficienciaEscudos { get; set; }
    }
}
