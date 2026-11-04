using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Copia uma ficha inteira (atributos, combate, itens, magias, ataques, ações...) para um personagem novo,
    /// com identificadores novos. É a base da "biblioteca de criaturas": o mesmo monstro pode entrar em várias campanhas.
    /// </summary>
    public static class PersonagemClonador
    {
        public static Personagem Clonar(Personagem origem, int campanhaId, string nome, int criadoPorId) => new()
        {
            Nome = nome,
            Descricao = origem.Descricao,
            CampanhaId = campanhaId,
            TipoId = origem.TipoId,
            CriadoPorId = criadoPorId,
            CriadoEm = DateTime.Now,
            Ativo = true,

            Atributo = Copiar(origem.Atributo)!,
            PersonagemPlayer = Copiar(origem.PersonagemPlayer),
            PersonagemCombate = Copiar(origem.PersonagemCombate),
            PersonagemDetalhes = Copiar(origem.PersonagemDetalhes),
            PersonagemConjuracao = Copiar(origem.PersonagemConjuracao),
            PersonagemDinheiro = Copiar(origem.PersonagemDinheiro),
            PersonagemNpc = Copiar(origem.PersonagemNpc),

            PersonagemItens = CopiarLista(origem.PersonagemItens),
            PersonagemMagias = CopiarLista(origem.PersonagemMagias),
            PersonagemAtaques = CopiarLista(origem.PersonagemAtaques),
            PersonagemAcoesPadrao = CopiarLista(origem.PersonagemAcoesPadrao),
            PersonagemAcoesBonus = CopiarLista(origem.PersonagemAcoesBonus),
            PersonagemReacoes = CopiarLista(origem.PersonagemReacoes),
            PersonagemAcoesLendarias = CopiarLista(origem.PersonagemAcoesLendarias),
            PersonagemTracosEspeciais = CopiarLista(origem.PersonagemTracosEspeciais),
            PersonagemPericias = origem.PersonagemPericias?.Select(p => new PersonagemPericia { PericiaId = p.PericiaId, Bonus = p.Bonus }).ToList(),
        };

        private static List<T>? CopiarLista<T>(IEnumerable<T>? origem) where T : class, new() =>
            origem?.Select(Copiar).Select(c => c!).ToList();

        /// <summary>Copia só os valores simples (texto, números, datas...) e deixa de fora as chaves e as ligações com outras entidades.</summary>
        private static T? Copiar<T>(T? origem) where T : class, new()
        {
            if (origem == null) return null;

            var novo = new T();
            foreach (var propriedade in typeof(T).GetProperties())
            {
                if (!propriedade.CanRead || !propriedade.CanWrite || !EhValorSimples(propriedade.PropertyType) || EhChave(propriedade, typeof(T)))
                    continue;
                propriedade.SetValue(novo, propriedade.GetValue(origem));
            }
            return novo;
        }

        private static bool EhValorSimples(Type tipo)
        {
            tipo = Nullable.GetUnderlyingType(tipo) ?? tipo;
            return tipo.IsPrimitive || tipo.IsEnum || tipo == typeof(string) || tipo == typeof(decimal) || tipo == typeof(DateTime) || tipo == typeof(Guid);
        }

        /// <summary>Chave primária própria ("PersonagemAtaqueId") ou a ligação com o personagem de origem ("PersonagemId").</summary>
        private static bool EhChave(PropertyInfo propriedade, Type dono) =>
            propriedade.GetCustomAttribute<KeyAttribute>() != null
            || propriedade.Name == "PersonagemId"
            || propriedade.Name == dono.Name + "Id";
    }
}
