using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Application.Services
{
    public static class TiposDeEvento
    {
        public static readonly string[] Todos = ["Marco", "Combate", "Descoberta", "Morte", "Viagem", "Diplomacia", "Descanso", "Outro"];

        public static bool Valido(string? tipo) => Todos.Contains(tipo);
    }

    public interface ICronicaService
    {
        /// <summary>
        /// Prepara um evento registrado pelo sistema (combate encerrado, descanso...) no fim da cronologia.
        /// Não grava: quem chama confirma junto com a sua própria mudança (CommitAsync), numa operação só.
        /// </summary>
        Task RegistrarAutomaticoAsync(int campanhaId, string tipo, string titulo, string descricao, int? sessaoId = null);
    }

    public class CronicaService : ICronicaService
    {
        private readonly IUnitOfWork _uof;

        public CronicaService(IUnitOfWork uof) => _uof = uof;

        public async Task RegistrarAutomaticoAsync(int campanhaId, string tipo, string titulo, string descricao, int? sessaoId = null)
        {
            var ordem = await _uof.EventoCampanhaRepository.MaiorOrdemAsync(campanhaId) + 1;
            await _uof.EventoCampanhaRepository.AddAsync(new EventoCampanha
            {
                CampanhaId = campanhaId,
                Titulo = titulo.Length > 120 ? titulo[..120] : titulo,
                Descricao = descricao.Length > 4000 ? descricao[..4000] : descricao,
                Tipo = tipo,
                SessaoId = sessaoId,
                Ordem = ordem,
                Automatico = true,
                CriadoEm = DateTime.UtcNow,
            });
        }
    }
}
