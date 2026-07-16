using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    public class PastaAnotacao
    {
        [Key]
        public int PastaId { get; set; }
        
        [Required]
        public int CampanhaId { get; set; }
        
        [JsonIgnore]
        public Campanha? Campanha { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool IsPublica { get; set; } = false;

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        [JsonIgnore]
        public ICollection<Anotacao> Anotacoes { get; set; } = new List<Anotacao>();
    }
}
