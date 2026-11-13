using System;
using System.Linq.Expressions;
using Wyvern.Domain.Entities;
using CampanhaEntity = Wyvern.Domain.Entities.Campanha;
using PersonagemEntity = Wyvern.Domain.Entities.Personagem;
using SessaoEntity = Wyvern.Domain.Entities.Sessao;

namespace Wyvern.Infrastructure.Repositories
{
    /// <summary>
    /// Quem vê o quê. As listas completas, as paginadas e o painel usam estas mesmas regras,
    /// para uma tela nunca mostrar algo que a outra esconde.
    /// </summary>
    internal static class Visibilidade
    {
        /// <summary>Campanhas ativas de que o usuário participa.</summary>
        public static Expression<Func<CampanhaEntity, bool>> DaCampanha(int? usuarioId) =>
            c => c.Ativo && c.Membros!.Any(m => m.UsuarioId == usuarioId);

        /// <summary>Sessões ativas de campanhas ativas de que o usuário participa.</summary>
        public static Expression<Func<SessaoEntity, bool>> DaSessao(int? usuarioId) =>
            s => s.Ativo && s.Campanha!.Ativo && s.Campanha.Membros!.Any(m => m.UsuarioId == usuarioId);

        /// <summary>O jogador vê os próprios personagens; o Mestre vê todos os da campanha.</summary>
        public static Expression<Func<PersonagemEntity, bool>> DoPersonagem(int? usuarioId) =>
            p => p.Ativo && p.Campanha.Membros!.Any(m => m.UsuarioId == usuarioId && (p.CriadoPorId == usuarioId || m.Papel == PapelCampanha.Mestre));
    }
}
