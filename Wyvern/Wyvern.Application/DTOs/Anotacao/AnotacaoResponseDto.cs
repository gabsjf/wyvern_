using System;

namespace Wyvern.Application.DTOs.Anotacao
{
    public class AnotacaoResponseDto
    {
        public int AnotacaoId { get; set; }
        public int CampanhaId { get; set; }
        public int? PastaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public bool IsPublica { get; set; }
        public DateTime CriadoEm { get; set; }
        public int? CriadoPorId { get; set; }
    }
}
