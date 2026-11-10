using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wyvern.Domain.Entities
{
    /// <summary>Foto de perfil, numa tabela à parte para os bytes não virem junto em toda consulta de usuário.</summary>
    [Table("UsuarioFoto")]
    public class UsuarioFoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required, StringLength(20)]
        public string Tipo { get; set; } = "image/png";

        public byte[] Dados { get; set; } = Array.Empty<byte>();
    }
}
