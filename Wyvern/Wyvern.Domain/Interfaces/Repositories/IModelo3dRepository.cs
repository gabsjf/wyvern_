using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Interfaces.Repositories
{
    /// <summary>A biblioteca de modelos 3D da campanha e as peças de cenário postas nos mapas.</summary>
    public interface IModelo3dRepository
    {
        Task<List<Modelo3d>> ListarAsync(int campanhaId);
        /// <summary>Rastreado (para apagar).</summary>
        Task<Modelo3d?> GetAsync(int campanhaId, int modeloId);
        Task<byte[]?> GetDadosAsync(int modeloId);
        /// <summary>Soma do tamanho dos modelos da campanha, em bytes.</summary>
        Task<long> EspacoUsadoAsync(int campanhaId);
        Task AddAsync(Modelo3d modelo, byte[] dados);
        void Remover(Modelo3d modelo);
        /// <summary>
        /// Põe `novo` (com estes bytes) no lugar de `antigo`: peças nos mapas, tokens e fichas passam a usar o novo, e o
        /// antigo some. Tudo no mesmo CommitAsync. Os bytes de um modelo nunca mudam, por isso é um modelo novo.
        /// </summary>
        Task SubstituirAsync(Modelo3d antigo, Modelo3d novo, byte[] dados);

        Task<List<ObjetoCena>> ListarObjetosAsync(int cenaId);
        /// <summary>Rastreado (para gravar).</summary>
        Task<ObjetoCena?> GetObjetoAsync(int cenaId, int objetoId);
        Task<int> ContarObjetosAsync(int cenaId);
        Task AddObjetoAsync(ObjetoCena objeto);
        void RemoverObjeto(ObjetoCena objeto);
        /// <summary>Cenas da campanha que usam o modelo (para avisar a mesa quando ele some).</summary>
        Task<List<int>> CenasQueUsamAsync(int modeloId);
    }
}
