using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    public class Anotacao
    {
        [Key]
        public int AnotacaoId { get; set; }
        
        [Required]
        public int CampanhaId { get; set; }
        
        [JsonIgnore]
        public Campanha? Campanha { get; set; }

        public int? PastaId { get; set; }
        
        [JsonIgnore]
        public PastaAnotacao? Pasta { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string Conteudo { get; set; } = string.Empty; // Markdown
        
        public bool IsPublica { get; set; } = false;
        
        public int? CriadoPorId { get; set; }
        [JsonIgnore]
        public Usuario? CriadoPor { get; set; }
        
        public DateTime CriadoEm { get; set; } = DateTime.Now;
    }
}
