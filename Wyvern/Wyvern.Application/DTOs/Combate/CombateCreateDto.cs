using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Wyvern.Application.DTOs.Combate
{
    public class CombateCreateDto
    {
        public int? SessaoId { get; set; }
        public int? CampanhaId { get; set; }
        
        public List<CombateParticipanteCreateDto>? Participantes { get; set; }
    }
}
