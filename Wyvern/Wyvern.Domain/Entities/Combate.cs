using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// A ordem dos turnos: maior iniciativa primeiro e, no empate, quem entrou antes. O desempate fixo importa:
        /// sem ele a ordem dependia de como o banco devolvia as linhas, e a vez podia cair em outra pessoa.
        /// </summary>
        public static List<CombateParticipante> Ordenar(IEnumerable<CombateParticipante> participantes) =>
            participantes.OrderByDescending(p => p.Iniciativa).ThenBy(p => p.ParticipanteId).ToList();

        /// <summary>O participante da vez, ou nulo se o combate não tem participantes.</summary>
        public CombateParticipante? ParticipanteDaVez()
        {
            if (Participantes == null) return null;
            var ordem = Ordenar(Participantes);
            return TurnoAtualIndex >= 0 && TurnoAtualIndex < ordem.Count ? ordem[TurnoAtualIndex] : null;
        }

        /// <summary>
        /// O turno em andamento ("combate:rodada:participante da vez"). O deslocamento gasto vale só dentro do mesmo
        /// turno: o que o token anda na vez dos outros não sai do que ele tem na própria vez.
        /// </summary>
        public string CarimboDoTurno() => $"{CombateId}:{RodadaAtual}:{ParticipanteDaVez()?.ParticipanteId}";
    }
}
