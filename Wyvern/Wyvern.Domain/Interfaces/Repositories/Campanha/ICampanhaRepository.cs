using System;
using System.Collections.Generic;
using System.Text;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;
using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Interfaces.Repositories.Campanha
{
   public interface ICampanhaRepository
    {
        /// <summary>Campanhas ativas de que o usuário atual participa.</summary>
        Task<IEnumerable<CampanhaEntity>> GetCampanhasAsync();
        /// <summary>Campanha ativa, desde que o usuário atual participe dela.</summary>
        Task<CampanhaEntity?> GetCampanhaAsync(int id);
        /// <summary>Cria a campanha e registra o usuário atual como Mestre dela.</summary>
        Task<CampanhaEntity> CreateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity?> GetCampanhaByTokenAsync(string token);
        Task<CampanhaEntity> UpdateCampanhaAsync(CampanhaEntity campanha);
        Task<CampanhaEntity> DeleteCampanhaAsync(int id);

        /// <summary>Papel do usuário na campanha ativa, ou nulo se não participa. Sem filtro pelo usuário atual: serve às decisões de autorização.</summary>
        Task<PapelCampanha?> ObterPapelAsync(int campanhaId, int usuarioId);

        /// <summary>Campanha ativa com membros (e seus usuários), rastreada para alteração e sem filtro pelo usuário atual. Quem chama deve checar a permissão.</summary>
        Task<CampanhaEntity?> GetCampanhaParaGestaoAsync(int campanhaId);

        /// <summary>Campanhas ativas de que o usuário é dono (criador): é o que o plano limita.</summary>
        Task<int> ContarCampanhasDoDonoAsync(int usuarioId);

        /// <summary>Campanhas ativas de que o usuário é dono ou membro, com membros e seus usuários, rastreadas para alteração.</summary>
        Task<List<CampanhaEntity>> GetCampanhasDoUsuarioParaGestaoAsync(int usuarioId);
    }
}
