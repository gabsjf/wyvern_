using AutoMapper;
using Wyvern.Application.DTOs.Sessao;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class SessaoProfile : Profile
    {
        public SessaoProfile()
        {
            CreateMap<Sessao, CreateSessaoDto>().ReverseMap();
            CreateMap<Sessao, UpdateSessaoDto>().ReverseMap();
            CreateMap<Sessao, SessaoResponseDto>().ReverseMap();
        }
    }
}
