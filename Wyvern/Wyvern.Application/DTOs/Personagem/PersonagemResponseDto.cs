using Wyvern.Application.DTOs.Atributo;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemResponseDto
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int CampanhaId { get; set; }
        /// <summary>Nome da campanha (vem na listagem; nulo onde a campanha não foi carregada).</summary>
        public string? CampanhaNome { get; set; }
        public int TipoId { get; set; }
        public int CriadoPorId { get; set; }
        /// <summary>Nome de quem criou o personagem (o jogador dono, ou o Mestre nos NPCs e monstros). Vem na listagem.</summary>
        public string? CriadoPorNome { get; set; }
        public DateTime CriadoEm { get; set; }
        public bool Ativo { get; set; }
        
        public AtributoResponseDto? Atributo { get; set; }
        public PersonagemPlayerResponseDto? PersonagemPlayer { get; set; }
        public PersonagemCombateResponseDto? PersonagemCombate { get; set; }
        public PersonagemDetalhesUpdateDto? PersonagemDetalhes { get; set; }
        public PersonagemDinheirosUpdateDto? PersonagemDinheiro { get; set; }
        public PersonagemConjuracaoDto? PersonagemConjuracao { get; set; }
        
        public List<PersonagemItemResponseDto>? PersonagemItens { get; set; }
        public List<PersonagemMagiaResponseDto>? PersonagemMagias { get; set; }
        public List<PersonagemAtaqueResponseDto>? PersonagemAtaques { get; set; }
        public List<PersonagemPericiaResponseDto>? PersonagemPericias { get; set; }
        
        public PersonagemNpcDto? PersonagemNpc { get; set; }
        public List<PersonagemAcaoPadraoDto>? PersonagemAcoesPadrao { get; set; }
        public List<PersonagemAcaoBonusDto>? PersonagemAcoesBonus { get; set; }
        public List<PersonagemReacaoDto>? PersonagemReacoes { get; set; }
        public List<PersonagemAcaoLendariaDto>? PersonagemAcoesLendarias { get; set; }
        public List<PersonagemTracoEspecialDto>? PersonagemTracosEspeciais { get; set; }
    }
}
