using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class CampanhaProfile : Profile
    {
        public CampanhaProfile() 
        { 
            CreateMap<Campanha, CreateCampanhaDto>().ReverseMap();
            CreateMap<Campanha, CampanhaResponseDetailDto>().ReverseMap();
            CreateMap<Campanha, CampanhaResponseDto>().ReverseMap();
            CreateMap<Campanha, CampanhaUpdatetDto>().ReverseMap();
        }
    }
}
