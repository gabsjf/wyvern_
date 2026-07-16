using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Atributo;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class AtributoProfile : Profile
    {
        public AtributoProfile()
        {
            CreateMap<Atributo,CreateAtributoDto>().ReverseMap();
            CreateMap<Atributo, AtributoResponseDto>().ReverseMap();
            CreateMap<Atributo, AtributoUpdateDto>().ReverseMap();
        }
    }
}
