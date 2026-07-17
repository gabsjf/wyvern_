using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    [Table("Campanhas_Jogadores")]
    public class CampanhaJogador
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CampanhaId { get; set; }
        [JsonIgnore]
        public Campanha? Campanha { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        [JsonIgnore]
        public Usuario? Usuario { get; set; }

        public DateTime EntrouEm { get; set; } = DateTime.Now;
    }
}
