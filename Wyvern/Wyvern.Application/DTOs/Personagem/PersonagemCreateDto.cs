using System.ComponentModel.DataAnnotations;
using Wyvern.Application.DTOs.Atributo;

namespace Wyvern.Application.DTOs.Personagem
{
    public class PersonagemCreateDto
    {
        [Required]
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        [Required]
        public int CampanhaId { get; set; }
        [Required]
        public int TipoId { get; set; }
        [Required]
        public int CriadoPorId { get; set; }
        public CreateAtributoDto? Atributo { get; set; }
        
        public PersonagemPlayerUpdateDto? PersonagemPlayer { get; set; }
        public PersonagemCombateUpdateDto? PersonagemCombate { get; set; }
        public PersonagemDetalhesUpdateDto? PersonagemDetalhes { get; set; }
        public PersonagemDinheirosUpdateDto? PersonagemDinheiro { get; set; }
        public PersonagemConjuracaoDto? PersonagemConjuracao { get; set; }
        
        public List<PersonagemAtaqueCreateDto>? PersonagemAtaques { get; set; }
        public List<PersonagemMagiaAddDto>? PersonagemMagias { get; set; }
        
        public PersonagemNpcDto? PersonagemNpc { get; set; }
        public List<PersonagemAcaoPadraoDto>? PersonagemAcoesPadrao { get; set; }
        public List<PersonagemAcaoBonusDto>? PersonagemAcoesBonus { get; set; }
        public List<PersonagemReacaoDto>? PersonagemReacoes { get; set; }
        public List<PersonagemAcaoLendariaDto>? PersonagemAcoesLendarias { get; set; }
        public List<PersonagemTracoEspecialDto>? PersonagemTracosEspeciais { get; set; }
        
        public List<int>? PericiasIds { get; set; }
    }
}
