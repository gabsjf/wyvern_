using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Pericia;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class PericiaProfile : Profile
    {
        public PericiaProfile()
        {
            CreateMap<Pericia, CreatePericiaDto>().ReverseMap();
            CreateMap<Pericia, PericiaUpdateDto>().ReverseMap();
            CreateMap<Pericia, PericiaResponseDto>().ReverseMap();
        }
    }
}
