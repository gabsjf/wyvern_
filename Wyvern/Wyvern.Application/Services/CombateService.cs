using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    public class CombateService : ICombateService
    {
        private readonly IUnitOfWork _uof;

        public CombateService(IUnitOfWork uof)
        {
            _uof = uof;
        }

        public bool ParticipanteEstaMorto(CombateParticipante participante)
        {
            return (participante.IsInimigo && participante.VidaAtual <= 0)
                || (!participante.IsInimigo && participante.FalhasMorte >= 3);
        }

        public async Task<Combate?> AvancarTurnoAsync(int combateId)
        {
            var combate = await _uof.CombateRepository.GetCombateAsync(combateId);
            if (combate == null || combate.Participantes == null || !combate.Participantes.Any())
                return combate;

            var participantesSorted = combate.Participantes.OrderByDescending(p => p.Iniciativa).ToList();
            int originalIndex = combate.TurnoAtualIndex;
            do
            {
                combate.TurnoAtualIndex++;
                if (combate.TurnoAtualIndex >= participantesSorted.Count)
                {
                    combate.TurnoAtualIndex = 0;
                    combate.RodadaAtual++;
                }

                var p = participantesSorted[combate.TurnoAtualIndex];
                if (!ParticipanteEstaMorto(p)) break;

            } while (combate.TurnoAtualIndex != originalIndex);

            await _uof.CombateRepository.UpdateCombateAsync(combate);
            await _uof.CommitAsync();

            return combate;
        }
    }
}
