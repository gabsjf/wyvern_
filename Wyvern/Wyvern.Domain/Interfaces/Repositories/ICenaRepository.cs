using Wyvern.Domain.Entities;

namespace Wyvern.Domain.Interfaces.Repositories.Cena
{
    public interface ICenaRepository
    {
        Task<List<Entities.Cena>> GetPorCampanhaAsync(int campanhaId);
        Task<Entities.Cena?> GetAsync(int campanhaId, int cenaId);
        Task<Entities.Cena?> GetAtivaAsync(int campanhaId);
        Task<int> MaiorOrdemAsync(int campanhaId);
        Task<CenaImagem?> GetImagemAsync(int cenaId);
        /// <summary>A imagem com a névoa, rastreada, para gravar alterações.</summary>
        Task<CenaImagem?> GetImagemParaEditarAsync(int cenaId);

        Task<List<TokenCena>> GetTokensAsync(int cenaId);
        /// <summary>O token com a cena e o personagem (para saber quem é o dono).</summary>
        Task<TokenCena?> GetTokenAsync(int cenaId, int tokenId);
        /// <summary>O token de uma cena da campanha, sem saber a cena de antemão (usado pelo hub).</summary>
        Task<TokenCena?> GetTokenDaCampanhaAsync(int campanhaId, int tokenId);

        Task<bool> PersonagemEhDaCampanhaAsync(int campanhaId, int personagemId);
        Task<bool> ParticipanteEhDaCampanhaAsync(int campanhaId, int participanteId);

        Task<TokenImagem?> GetTokenImagemAsync(int tokenId);
        Task<TokenImagem?> GetTokenImagemParaEditarAsync(int tokenId);
        Task AddTokenImagemAsync(TokenImagem imagem);
        Task RemoveTokenImagemAsync(TokenImagem imagem);

        Task<List<ParedeCena>> GetParedesAsync(int cenaId);
        Task<ParedeCena?> GetParedeAsync(int cenaId, int paredeId);
        Task<int> ContarParedesAsync(int cenaId);
        Task AddParedeAsync(ParedeCena parede);
        Task AddParedesAsync(IEnumerable<ParedeCena> paredes);
        Task RemoveParedeAsync(ParedeCena parede);

        Task<List<MarcadorCena>> GetMarcadoresAsync(int cenaId);
        Task<MarcadorCena?> GetMarcadorAsync(int cenaId, int marcadorId);
        Task AddMarcadorAsync(MarcadorCena marcador);
        Task AddMarcadoresAsync(IEnumerable<MarcadorCena> marcadores);
        Task RemoveMarcadorAsync(MarcadorCena marcador);

        Task<List<TracoCena>> GetTracosAsync(int cenaId);
        Task<TracoCena?> GetTracoAsync(int cenaId, int tracoId);
        Task<int> ContarTracosAsync(int cenaId);
        Task AddTracoAsync(TracoCena traco);
        Task RemoveTracoAsync(TracoCena traco);
        Task RemoveTodosOsTracosAsync(int cenaId);

        Task AddAsync(Entities.Cena cena, CenaImagem imagem);
        Task AddTokensAsync(IEnumerable<TokenCena> tokens);
        Task RemoveAsync(Entities.Cena cena);
        Task RemoveTokenAsync(TokenCena token);
    }
}
