using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Wyvern.Domain.Entities;

namespace Wyvern.Application.DTOs.Cena
{
    public static class PaletaDeTokens
    {
        /// <summary>Chaves das cores de token; quem desenha (o front) traduz cada uma numa cor.</summary>
        public static readonly string[] Cores = ["ouro", "carmim", "verde", "azul", "violeta", "laranja", "cinza", "branco"];

        public static bool Valida(string? cor) => Cores.Contains(cor);
    }

    /// <summary>Criação de cena: multipart, porque leva a imagem do mapa.</summary>
    public class CenaFormDto
    {
        [Required, StringLength(80, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Tamanho da imagem em pixels (o navegador mede ao escolher o arquivo).</summary>
        [Range(100, 16000)]
        public int Largura { get; set; }
        [Range(100, 16000)]
        public int Altura { get; set; }

        /// <summary>Lado de um quadrado da grade, em pixels da imagem.</summary>
        [Range(16, 1000)]
        public int TamanhoCelula { get; set; } = 70;

        /// <summary>Sessão da mesa a que o mapa pertence (opcional; precisa ser da mesma campanha).</summary>
        public int? SessaoId { get; set; }

        [Required]
        public IFormFile? Imagem { get; set; }
    }

    public class CenaEdicaoDto
    {
        [Required, StringLength(80, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        [Range(16, 1000)]
        public int TamanhoCelula { get; set; } = 70;

        public int? SessaoId { get; set; }
        public bool SoNoTurno { get; set; } = true;

        /// <summary>Metros por quadrado (5 pés = 1,5 m).</summary>
        [Range(0.1, 100)]
        public double MetrosPorQuadrado { get; set; } = 1.5;

        public bool LimitarDeslocamento { get; set; } = true;
    }

    public class CenaDto
    {
        public int CenaId { get; set; }
        public int CampanhaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Largura { get; set; }
        public int Altura { get; set; }
        public int TamanhoCelula { get; set; }
        public bool Ativa { get; set; }
        public int Ordem { get; set; }
        public int ImagemVersao { get; set; }
        public bool NeblinaAtiva { get; set; }
        public int? SessaoId { get; set; }
        public bool SoNoTurno { get; set; }
        public double MetrosPorQuadrado { get; set; }
        public bool LimitarDeslocamento { get; set; }

        public static CenaDto De(Domain.Entities.Cena c) => new()
        {
            CenaId = c.CenaId, CampanhaId = c.CampanhaId, Nome = c.Nome, Largura = c.Largura, Altura = c.Altura,
            TamanhoCelula = c.TamanhoCelula, Ativa = c.Ativa, Ordem = c.Ordem, ImagemVersao = c.ImagemVersao,
            NeblinaAtiva = c.NeblinaAtiva, SessaoId = c.SessaoId, SoNoTurno = c.SoNoTurno,
            MetrosPorQuadrado = MovimentoRegras.MetrosPorQuadrado(c), LimitarDeslocamento = c.LimitarDeslocamento,
        };
    }

    public class TokenFormDto
    {
        [Required, StringLength(60, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        [Range(0, 1000)] public int Coluna { get; set; }
        [Range(0, 1000)] public int Linha { get; set; }
        [Range(1, 4)] public int Tamanho { get; set; } = 1;
        public string Cor { get; set; } = "ouro";
        public bool Inimigo { get; set; }
        public bool Oculto { get; set; }
        /// <summary>Raio de visão em pés (0 = nenhum).</summary>
        [Range(0, 300)] public int Visao { get; set; }
        /// <summary>Deslocamento à mão, em metros; nulo usa o da ficha.</summary>
        [Range(0, 500)] public double? DeslocamentoMetros { get; set; }
        /// <summary>Zera o que o token já andou nesta rodada.</summary>
        public bool RestaurarMovimento { get; set; }
        public int? PersonagemId { get; set; }
        public int? ParticipanteId { get; set; }
        /// <summary>Miniatura 3D só deste token; nula usa a do personagem.</summary>
        public int? ModeloId { get; set; }
        public int? ControladoPorId { get; set; }
    }

    public class TokenCenaDto
    {
        public int TokenId { get; set; }
        public int CenaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Coluna { get; set; }
        public int Linha { get; set; }
        public int Tamanho { get; set; }
        /// <summary>Para onde a miniatura olha, em graus (ver TokenCena.Rotacao).</summary>
        public double Rotacao { get; set; }
        public string Cor { get; set; } = "ouro";
        public bool Inimigo { get; set; }
        public bool Oculto { get; set; }
        public int Visao { get; set; }
        public int ImagemVersao { get; set; }
        /// <summary>Deslocamento que vale (o manual, senão o da ficha), em metros; nulo = sem limite conhecido.</summary>
        public double? DeslocamentoMetros { get; set; }
        /// <summary>O que está escrito à mão no token (nulo = usa a ficha).</summary>
        public double? DeslocamentoManual { get; set; }
        /// <summary>O que a ficha do personagem diz, em metros (nulo = sem ficha ou ilegível).</summary>
        public double? DeslocamentoDaFicha { get; set; }
        public int MovimentoGasto { get; set; }
        public string? MovimentoTurno { get; set; }
        public int? PersonagemId { get; set; }
        public int? ParticipanteId { get; set; }
        public int? ControladoPorId { get; set; }
        /// <summary>Miniatura escolhida no próprio token (nula = herda do personagem).</summary>
        public int? ModeloId { get; set; }
        /// <summary>A miniatura que vale na visão 3D: a do token, senão a do personagem.</summary>
        public int? MiniaturaId { get; set; }

        /// <summary>Usuários (fora o Mestre) que podem mover o token: quem controla e o dono do personagem.</summary>
        public int[] DonosIds { get; set; } = [];

        private static double? DaFicha(TokenCena t) => MovimentoRegras.DaFicha(t.Personagem);

        public static TokenCenaDto De(TokenCena t) => new()
        {
            TokenId = t.TokenId, CenaId = t.CenaId, Nome = t.Nome, Coluna = t.Coluna, Linha = t.Linha, Tamanho = t.Tamanho, Rotacao = t.Rotacao,
            Cor = t.Cor, Inimigo = t.Inimigo, Oculto = t.Oculto, Visao = t.Visao, ImagemVersao = t.ImagemVersao, DeslocamentoManual = t.DeslocamentoMetros, DeslocamentoDaFicha = DaFicha(t),
            DeslocamentoMetros = t.DeslocamentoMetros ?? DaFicha(t), MovimentoGasto = t.MovimentoGasto, MovimentoTurno = t.MovimentoTurno, PersonagemId = t.PersonagemId,
            ParticipanteId = t.ParticipanteId, ControladoPorId = t.ControladoPorId, DonosIds = CenaRegras.Donos(t),
            ModeloId = t.ModeloId, MiniaturaId = t.ModeloId ?? t.Personagem?.ModeloId,
        };
    }

    public class ParedeFormDto
    {
        [Range(0, 1000)] public int X1 { get; set; }
        [Range(0, 1000)] public int Y1 { get; set; }
        [Range(0, 1000)] public int X2 { get; set; }
        [Range(0, 1000)] public int Y2 { get; set; }
        public bool Porta { get; set; }
        public bool Aberta { get; set; }
        public bool Janela { get; set; }
    }

    public class ParedeDto
    {
        public int ParedeId { get; set; }
        public int CenaId { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public bool Porta { get; set; }
        public bool Aberta { get; set; }
        public bool Janela { get; set; }

        public static ParedeDto De(ParedeCena p) => new()
        {
            ParedeId = p.ParedeId, CenaId = p.CenaId, X1 = p.X1, Y1 = p.Y1, X2 = p.X2, Y2 = p.Y2, Porta = p.Porta, Aberta = p.Aberta, Janela = p.Janela,
        };
    }

    public static class IconesDeMarcador
    {
        /// <summary>Chaves dos ícones de marcador; quem desenha (o front) traduz cada uma num símbolo.</summary>
        public static readonly string[] Todos =
            ["pino", "armadilha", "tesouro", "perigo", "segredo", "altar", "bau", "pergunta", "caveira", "chave"];

        public static bool Valido(string? icone) => Todos.Contains(icone);
    }

    public class MarcadorFormDto
    {
        [Range(0, 1000)] public int Coluna { get; set; }
        [Range(0, 1000)] public int Linha { get; set; }
        public string Icone { get; set; } = "pino";
        [Required, StringLength(80, MinimumLength = 1)] public string Titulo { get; set; } = string.Empty;
        [StringLength(1000)] public string? Nota { get; set; }
        public bool Visivel { get; set; }
    }

    public class MarcadorDto
    {
        public int MarcadorId { get; set; }
        public int CenaId { get; set; }
        public int Coluna { get; set; }
        public int Linha { get; set; }
        public string Icone { get; set; } = "pino";
        public string Titulo { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;
        public bool Visivel { get; set; }

        public static MarcadorDto De(MarcadorCena m) => new()
        {
            MarcadorId = m.MarcadorId, CenaId = m.CenaId, Coluna = m.Coluna, Linha = m.Linha, Icone = m.Icone,
            Titulo = m.Titulo, Nota = m.Nota, Visivel = m.Visivel,
        };
    }

    public class TracoFormDto
    {
        public string Cor { get; set; } = "ouro";
        [Range(1, 12)] public int Espessura { get; set; } = 3;
        /// <summary>Pontos [x, y] em pixels da imagem (de 2 a 600).</summary>
        [Required] public List<double[]>? Pontos { get; set; }
    }

    public class TracoDto
    {
        public int TracoId { get; set; }
        public int CenaId { get; set; }
        public int? UsuarioId { get; set; }
        public string Cor { get; set; } = "ouro";
        public int Espessura { get; set; }
        public List<double[]> Pontos { get; set; } = new();

        public static TracoDto De(TracoCena t) => new()
        {
            TracoId = t.TracoId, CenaId = t.CenaId, UsuarioId = t.UsuarioId, Cor = t.Cor, Espessura = t.Espessura, Pontos = LerPontos(t.Pontos),
        };

        public static string EscreverPontos(IEnumerable<double[]> pontos) =>
            string.Join(';', pontos.Select(p => $"{Math.Round(p[0], 1).ToString(System.Globalization.CultureInfo.InvariantCulture)},{Math.Round(p[1], 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

        public static List<double[]> LerPontos(string texto) =>
            texto.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(par => par.Split(','))
                .Where(v => v.Length == 2)
                .Select(v => new[]
                {
                    double.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture),
                })
                .ToList();
    }

    /// <summary>Quem pode mexer em quê no mapa e onde um token pode ficar. Compartilhado pela API e pelo hub.</summary>
    public static class CenaRegras
    {
        public static int[] Donos(TokenCena t)
        {
            var donos = new List<int>();
            if (t.ControladoPorId is { } c) donos.Add(c);
            if (t.Personagem is { } p && p.CriadoPorId != 0 && !donos.Contains(p.CriadoPorId)) donos.Add(p.CriadoPorId);
            return donos.ToArray();
        }

        /// <summary>O Mestre move qualquer token; o jogador só os visíveis dele, na cena que está em jogo.</summary>
        /// <summary>Qualquer ângulo vira 0 a 360 (e um valor inválido, 0).</summary>
        public static double Angulo(double graus) => double.IsFinite(graus) ? ((graus % 360) + 360) % 360 : 0;

        public static bool PodeMover(TokenCena t, PapelCampanha papel, int usuarioId) =>
            papel == PapelCampanha.Mestre
            || (!t.Oculto && t.Cena is { Ativa: true } && Donos(t).Contains(usuarioId));

        /// <summary>
        /// Com "só no turno" ligado e um combate em andamento, o jogador só move o token de quem está na vez.
        /// Tokens sem combatente e cenas sem a regra continuam livres. O Mestre nunca é limitado.
        /// </summary>
        public static bool NaVez(Domain.Entities.Cena cena, TokenCena token, Domain.Entities.Combate? combate)
        {
            if (!cena.SoNoTurno || combate?.Participantes == null || token.ParticipanteId == null) return true;
            // um combatente que não está neste combate (o combate dele acabou) não tem vez a respeitar
            if (combate.Participantes.All(p => p.ParticipanteId != token.ParticipanteId)) return true;
            return combate.ParticipanteDaVez()?.ParticipanteId == token.ParticipanteId;
        }

        private static double Lado(double ax, double ay, double bx, double by, double cx, double cy) =>
            (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);

        /// <summary>O ponto P, que já está na reta de A a B, cai dentro do segmento?</summary>
        private static bool Dentro(double ax, double ay, double bx, double by, double px, double py) =>
            px >= Math.Min(ax, bx) && px <= Math.Max(ax, bx) && py >= Math.Min(ay, by) && py <= Math.Max(ay, by);

        /// <summary>
        /// A parede C-D barra o passo A-B? Cruzar, ter uma ponta sobre o caminho (fora da partida) ou o destino
        /// sobre a parede barram; a partida nunca conta, porque dali o token já está saindo.
        /// </summary>
        public static bool Barra(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            var d1 = Lado(ax, ay, bx, by, cx, cy);
            var d2 = Lado(ax, ay, bx, by, dx, dy);
            var d3 = Lado(cx, cy, dx, dy, ax, ay);
            var d4 = Lado(cx, cy, dx, dy, bx, by);
            if (d1 * d2 < 0 && d3 * d4 < 0) return true; // cruza de verdade
            if (d1 == 0 && Dentro(ax, ay, bx, by, cx, cy) && !(cx == ax && cy == ay)) return true;
            if (d2 == 0 && Dentro(ax, ay, bx, by, dx, dy) && !(dx == ax && dy == ay)) return true;
            return d4 == 0 && Dentro(cx, cy, dx, dy, bx, by);
        }

        /// <summary>Mantém o token (de qualquer tamanho) inteiro dentro do mapa.</summary>
        public static (int coluna, int linha) Limitar(Domain.Entities.Cena cena, int tamanho, int coluna, int linha)
        {
            var colunas = Math.Max(1, cena.Largura / Math.Max(1, cena.TamanhoCelula));
            var linhas = Math.Max(1, cena.Altura / Math.Max(1, cena.TamanhoCelula));
            return (Math.Clamp(coluna, 0, Math.Max(0, colunas - tamanho)), Math.Clamp(linha, 0, Math.Max(0, linhas - tamanho)));
        }
    }
}
