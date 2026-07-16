using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Magia;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class MagiaProfile : Profile
    {
        public MagiaProfile() 
        {
            CreateMap<Magia, MagiaCreateDto>().ReverseMap();
            CreateMap<Magia, MagiaResponseDto>().ReverseMap();
            CreateMap<Magia, MagiaUpdateDto>().ReverseMap();
        }
    }
}
