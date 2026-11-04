using System.ComponentModel.DataAnnotations;

namespace Wyvern.Domain.Entities
{
    /// <summary>Um mapa tático da campanha: a imagem que o Mestre sobe, com a grade quadriculada por cima.</summary>
    public class Cena
    {
        [Key]
        public int CenaId { get; set; }

        public int CampanhaId { get; set; }
        public Campanha? Campanha { get; set; }

        [Required, StringLength(80)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Tamanho da imagem, em pixels.</summary>
        public int Largura { get; set; }
        public int Altura { get; set; }

        /// <summary>Lado de um quadrado da grade (5 pés), em pixels da imagem.</summary>
        public int TamanhoCelula { get; set; } = 70;

        /// <summary>A cena que os jogadores veem. No máximo uma por campanha.</summary>
        public bool Ativa { get; set; }

        public int Ordem { get; set; }

        /// <summary>Com névoa, os jogadores só veem os quadrados que o Mestre revelou.</summary>
        public bool NeblinaAtiva { get; set; }

        /// <summary>Sessão da mesa a que o mapa pertence (organização; pode ficar sem).</summary>
        public int? SessaoId { get; set; }
        public Sessao? Sessao { get; set; }

        /// <summary>No combate, o jogador só move o token de quem está na vez.</summary>
        public bool SoNoTurno { get; set; }

        /// <summary>Quantos metros vale um quadrado da grade (5 pés = 1,5 m no D&D).</summary>
        public double MetrosPorQuadrado { get; set; } = 1.5;

        /// <summary>No combate, o jogador só anda o deslocamento da ficha por rodada.</summary>
        public bool LimitarDeslocamento { get; set; } = true;

        /// <summary>Muda a cada troca de imagem; serve de ETag para o navegador guardar o mapa.</summary>
        public int ImagemVersao { get; set; } = 1;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public List<TokenCena> Tokens { get; set; } = new();
    }

    /// <summary>Os bytes da imagem da cena, em tabela própria para a listagem de cenas não carregá-los.</summary>
    public class CenaImagem
    {
        [Key]
        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        [Required, StringLength(20)]
        public string Tipo { get; set; } = "image/png";

        public byte[] Dados { get; set; } = Array.Empty<byte>();

        /// <summary>Quadrados revelados, um bit por quadrado (linha a linha). Nulo = nada revelado.</summary>
        public byte[]? Neblina { get; set; }
    }

    /// <summary>O retrato de um token, em tabela própria para a lista de tokens não carregá-lo.</summary>
    public class TokenImagem
    {
        [Key]
        public int TokenId { get; set; }
        public TokenCena? Token { get; set; }

        [Required, StringLength(20)]
        public string Tipo { get; set; } = "image/webp";

        public byte[] Dados { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Um segmento de parede (ou porta) entre dois cruzamentos da grade. Bloqueia a visão dos tokens;
    /// a porta só bloqueia enquanto estiver fechada.
    /// </summary>
    public class ParedeCena
    {
        [Key]
        public int ParedeId { get; set; }

        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        /// <summary>Extremidades, em cruzamentos da grade (0 = borda esquerda/superior do mapa).</summary>
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public bool Porta { get; set; }
        public bool Aberta { get; set; }

        /// <summary>Bloqueia o movimento, mas não a visão (grade, janela, balcão).</summary>
        public bool Janela { get; set; }
    }

    /// <summary>Um ponto de interesse no mapa (armadilha, tesouro, porta secreta...) com título e nota.</summary>
    public class MarcadorCena
    {
        [Key]
        public int MarcadorId { get; set; }

        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        /// <summary>Quadrado em que o marcador está.</summary>
        public int Coluna { get; set; }
        public int Linha { get; set; }

        /// <summary>Chave do ícone (ver IconesDeMarcador); o desenho fica no front.</summary>
        [Required, StringLength(20)]
        public string Icone { get; set; } = "pino";

        [Required, StringLength(80)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Nota { get; set; } = string.Empty;

        /// <summary>Só o Mestre vê até ele revelar o marcador à mesa.</summary>
        public bool Visivel { get; set; }
    }

    /// <summary>Um traço de desenho livre sobre o mapa, de quem o fez.</summary>
    public class TracoCena
    {
        [Key]
        public int TracoId { get; set; }

        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required, StringLength(12)]
        public string Cor { get; set; } = "ouro";

        public int Espessura { get; set; } = 3;

        /// <summary>Pontos em pixels da imagem, no formato "x,y;x,y;...".</summary>
        [Required, StringLength(16000)]
        public string Pontos { get; set; } = string.Empty;
    }

    /// <summary>Uma peça no mapa: personagem, NPC ou monstro. A posição é em quadrados da grade.</summary>
    public class TokenCena
    {
        [Key]
        public int TokenId { get; set; }

        public int CenaId { get; set; }
        public Cena? Cena { get; set; }

        [Required, StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        public int Coluna { get; set; }
        public int Linha { get; set; }

        /// <summary>Lado, em quadrados (1 = médio, 2 = grande, 3 = enorme, 4 = imenso).</summary>
        public int Tamanho { get; set; } = 1;

        /// <summary>Chave da paleta de cores (ver PaletaDeTokens); o desenho fica no front.</summary>
        [StringLength(12)]
        public string Cor { get; set; } = "ouro";

        public bool Inimigo { get; set; }

        /// <summary>Raio de visão em pés (0 = não enxerga): com névoa ligada, abre um círculo ao redor do token.</summary>
        public int Visao { get; set; }

        /// <summary>0 = sem imagem (o token mostra as iniciais). Muda a cada troca, para o navegador renovar o cache.</summary>
        public int ImagemVersao { get; set; }

        /// <summary>Deslocamento em metros definido à mão; nulo = vale o da ficha do personagem.</summary>
        public double? DeslocamentoMetros { get; set; }

        /// <summary>Quadrados já andados na rodada indicada por MovimentoTurno.</summary>
        public int MovimentoGasto { get; set; }

        /// <summary>"combateId:rodada" a que o gasto se refere; ao mudar a rodada o gasto volta a zero.</summary>
        [StringLength(40)]
        public string? MovimentoTurno { get; set; }

        /// <summary>Escondido dos jogadores: eles nem recebem o token.</summary>
        public bool Oculto { get; set; }

        public int? PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }

        /// <summary>Combatente que este token representa (PV, condições e turno vêm do combate).</summary>
        public int? ParticipanteId { get; set; }
        public CombateParticipante? Participante { get; set; }

        /// <summary>Jogador autorizado a mover o token, além do dono do personagem e do Mestre.</summary>
        public int? ControladoPorId { get; set; }
        public Usuario? ControladoPor { get; set; }
    }
}
