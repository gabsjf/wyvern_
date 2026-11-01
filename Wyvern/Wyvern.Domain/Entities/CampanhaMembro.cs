using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    /// <summary>Participação de um usuário numa campanha, com a função (papel) que ele exerce nela.</summary>
    [Table("Campanhas_Membros")]
    public class CampanhaMembro
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

        public PapelCampanha Papel { get; set; } = PapelCampanha.Jogador;

        public DateTime EntrouEm { get; set; } = DateTime.Now;
    }
}
