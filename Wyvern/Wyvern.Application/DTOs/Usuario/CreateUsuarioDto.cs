using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Campanha;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Usuario
{
    public class CreateUsuarioDto
    {
        public string Nome { get; set; }
        public string Email { get; set; }
    }
}
