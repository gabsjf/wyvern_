using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    public interface ICombateService
    {
        bool ParticipanteEstaMorto(CombateParticipante participante);
        Task<Combate?> AvancarTurnoAsync(int combateId);
    }
}
