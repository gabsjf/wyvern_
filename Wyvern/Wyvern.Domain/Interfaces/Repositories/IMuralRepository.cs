using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Interfaces.Repositories
{
    /// <summary>Um mural na lista: sem o documento, que pode ser grande.</summary>
    public record MuralResumo(int MuralId, string Nome, DateTime AtualizadoEm);

    public interface IMuralRepository
    {
        Task<List<MuralResumo>> ListarAsync(int campanhaId);
        /// <summary>O mural da campanha, rastreado (para gravar).</summary>
        Task<Mural?> GetAsync(int campanhaId, int muralId);
        Task AddAsync(Mural mural);
        void Remover(Mural mural);

        Task<MuralImagem?> GetImagemAsync(int muralId, int imagemId);
        Task AddImagemAsync(MuralImagem imagem);
        Task<int> ContarImagensAsync(int muralId);
        /// <summary>Apaga as imagens do mural que o documento não usa mais e que têm mais de <paramref name="anterioresA"/>.</summary>
        Task<int> RemoverImagensSemUsoAsync(int muralId, IReadOnlyCollection<int> emUso, DateTime anterioresA);
    }
}
