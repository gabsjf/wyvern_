using AutoMapper;
using Wyvern.Application.DTOs.Usuario;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class UsuarioProfile : Profile
    {
        public UsuarioProfile()
        {
            CreateMap<Usuario, CreateUsuarioDto>().ReverseMap();
            CreateMap<Usuario, UsuarioUpdateDto>().ReverseMap();
            CreateMap<Usuario, UsuarioResponseDto>().ReverseMap();
        }
    }
}
