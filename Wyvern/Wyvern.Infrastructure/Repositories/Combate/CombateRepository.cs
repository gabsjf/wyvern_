using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CombateEntity = Wyvern.Domain.Entities.Combate;
using Wyvern.Infrastructure.Data;

namespace Wyvern.Infrastructure.Repositories.Combate
{
    public class CombateRepository : ICombateRepository
    {
        private readonly WyvernDbContext _context;

        public CombateRepository(WyvernDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CombateEntity>> GetCombatesAsync(int sessaoId)
        {
            return await _context.Combates
                .Include(c => c.Participantes)
                .Where(c => c.SessaoId == sessaoId && c.Ativo)
                .ToListAsync();
        }

        public async Task<CombateEntity?> GetCombateAsync(int id)
        {
            return await _context.Combates
                .Include(c => c.Participantes)
                    .ThenInclude(p => p.Personagem)
                .FirstOrDefaultAsync(c => c.CombateId == id && c.Ativo);
        }

        public async Task<CombateEntity?> GetActiveCombateBySessaoAsync(int sessaoId)
        {
            return await _context.Combates
                .Include(c => c.Participantes)
                .Where(c => c.SessaoId == sessaoId && c.Ativo)
                .FirstOrDefaultAsync();
        }

        public async Task CreateCombateAsync(CombateEntity combate)
        {
            if (combate is null)
                throw new ArgumentNullException(nameof(combate));

            await _context.Combates.AddAsync(combate);
            await _context.SaveChangesAsync();
        }

        public async Task<CombateEntity> UpdateCombateAsync(CombateEntity combate)
        {
            if (combate is null)
                throw new ArgumentNullException(nameof(combate));

            _context.Entry(combate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return combate;
        }

        public async Task<CombateEntity> DeleteCombateAsync(int id)
        {
            var combate = await _context.Combates.FindAsync(id);

            if (combate is null)
                throw new ArgumentNullException(nameof(combate));

            combate.Ativo = false;
            await _context.SaveChangesAsync();

            return combate;
        }

        public async Task<CombateEntity?> GetActiveCombateByCampanhaAsync(int campanhaId)
        {
            return await _context.Combates
                .Include(c => c.Participantes)
                .Include(c => c.Sessao)
                .Where(c => c.Sessao != null && c.Sessao.CampanhaId == campanhaId && c.Ativo)
                .FirstOrDefaultAsync();
        }

        public async Task<Wyvern.Domain.Entities.CombateParticipante> UpdateParticipanteAsync(Wyvern.Domain.Entities.CombateParticipante participante)
        {
            if (participante is null)
                throw new ArgumentNullException(nameof(participante));

            _context.Entry(participante).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return participante;
        }
    }
}
