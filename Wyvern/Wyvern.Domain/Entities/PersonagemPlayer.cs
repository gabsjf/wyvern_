using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Wyvern.Domain.Entities
{
    public class PersonagemPlayer
    {
        [Key]
        [ForeignKey("personagem")]
        public int PersonagemId { get; set; }
        public Personagem personagem { get; set; }

        [StringLength(50)] // Otimização para o banco
        [Required]
        public string Classe { get; set; }
        [Required]
        public string Raca { get; set; }
        public int Nivel { get; set; }
        public int Xp { get; set; }
        public string Alinhamento { get; set; }
        public string? Antecedente { get; set; }
        public string? Subclasse { get; set; }
        public string? Tamanho { get; set; }

        /// <summary>
        /// Nível que o Mestre liberou por marco (sem depender do XP). O personagem pode subir até o maior
        /// entre este valor e o nível que o XP alcança. Zero quando o Mestre não liberou nada por marco.
        /// </summary>
        public int NivelLiberado { get; set; }
    }
}
