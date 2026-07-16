using AutoMapper;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class PersonagemProfile : Profile
    {
        public PersonagemProfile()
        {
            CreateMap<Personagem, PersonagemResponseDto>().ReverseMap();
            CreateMap<Personagem, PersonagemCreateDto>().ReverseMap();
            CreateMap<Personagem, PersonagemUpdateDto>().ReverseMap();

            CreateMap<PersonagemCombate, PersonagemCombateResponseDto>().ReverseMap();
            CreateMap<PersonagemCombate, PersonagemCombateUpdateDto>().ReverseMap();

            CreateMap<PersonagemPlayer, PersonagemPlayerResponseDto>().ReverseMap();
            CreateMap<PersonagemPlayer, PersonagemPlayerUpdateDto>().ReverseMap();

            CreateMap<PersonagemItem, PersonagemItemAddDto>().ReverseMap();
            CreateMap<PersonagemMagia, PersonagemMagiaAddDto>().ReverseMap();
            CreateMap<PersonagemMagia, PersonagemMagiaResponseDto>()
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Descricao, opt => opt.MapFrom(src => src.Descricao))
                .ForMember(dest => dest.Nivel, opt => opt.MapFrom(src => src.Nivel));
                
            CreateMap<PersonagemItem, PersonagemItemResponseDto>()
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.TipoItem, opt => opt.MapFrom(src => src.TipoItem))
                .ForMember(dest => dest.Raridade, opt => opt.MapFrom(src => src.Raridade))
                .ForMember(dest => dest.Descricao, opt => opt.MapFrom(src => src.Descricao));
                
            CreateMap<PersonagemAtaque, PersonagemAtaqueResponseDto>().ReverseMap();
            CreateMap<PersonagemAtaque, PersonagemAtaqueCreateDto>().ReverseMap();

            CreateMap<PersonagemPericia, PersonagemPericiaAddDto>().ReverseMap();
            CreateMap<PersonagemPericia, PersonagemPericiaUpdateDto>().ReverseMap();
            CreateMap<PersonagemPericia, PersonagemPericiaResponseDto>().ReverseMap();

            CreateMap<PersonagemDetalhes, PersonagemDetalhesUpdateDto>().ReverseMap();
            CreateMap<PersonagemDinheiro, PersonagemDinheirosUpdateDto>().ReverseMap();

            CreateMap<PersonagemNpc, PersonagemNpcDto>().ReverseMap();
            CreateMap<PersonagemAcaoPadrao, PersonagemAcaoPadraoDto>().ReverseMap();
            CreateMap<PersonagemAcaoBonus, PersonagemAcaoBonusDto>().ReverseMap();
            CreateMap<PersonagemReacao, PersonagemReacaoDto>().ReverseMap();
            CreateMap<PersonagemAcaoLendaria, PersonagemAcaoLendariaDto>().ReverseMap();
            CreateMap<PersonagemTracoEspecial, PersonagemTracoEspecialDto>().ReverseMap();
            CreateMap<PersonagemConjuracao, PersonagemConjuracaoDto>().ReverseMap();
        }
    }
}
