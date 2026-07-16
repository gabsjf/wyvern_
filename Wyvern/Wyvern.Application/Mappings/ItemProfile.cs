using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Atributo;
using Wyvern.Application.DTOs.Item;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Mappings
{
    public class ItemProfile : Profile
    {
        public ItemProfile()
        {
            CreateMap<Item, CreateItemDto>().ReverseMap();
            CreateMap<Item, ItemResponseDto>().ReverseMap();
            CreateMap<Item, ItemUpdateDto>().ReverseMap();

        }
    }
}
