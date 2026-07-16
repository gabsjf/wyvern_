using System;
using System.Collections.Generic;
using System.Text;
using Wyvern.Application.DTOs.Campanha;

namespace Wyvern.Application.DTOs.Usuario
{
    public class UsuarioResponseDto
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public List<CampanhaResponseDto>? Campanhas { get; set; }
    }
}
