using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    public class Sessao
    {
        [Key]
        public int SessaoId { get; set; }
        public int NumeroSessao { get; set; }
        public string Nome { get; set; }
        public DateTime DataSessao { get; set; }
        public DateTime? DataAgendada { get; set; }
        public string Obs { get; set; }
        public int CampanhaId { get; set; }
        [JsonIgnore]
        public Campanha? Campanha { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
