using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wyvern.Domain.Entities
{
    public class Combate
    {
        [Key]
        public int CombateId { get; set; }
        public int SessaoId { get; set; }
        [JsonIgnore]
        public Sessao? Sessao { get; set; }
        public int RodadaAtual { get; set; } = 1;
        public int TurnoAtualIndex { get; set; } = 0;
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public List<CombateParticipante>? Participantes { get; set; }
    }
}
