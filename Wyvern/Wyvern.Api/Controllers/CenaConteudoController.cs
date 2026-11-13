using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Wyvern.Api.Hubs;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    public class TokenImagemFormDto
    {
        [Required]
        public IFormFile? Imagem { get; set; }
    }

    /// <summary>
    /// O que mora dentro de uma cena além dos tokens: paredes e portas (que bloqueiam a visão), o retrato dos tokens
    /// e a duplicação da cena inteira. Mesmas regras do mapa: membros leem, só o Mestre escreve.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Cenas/{cenaId:int}")]
    public class CenaConteudoController : ControllerBase
    {
        private const long TamanhoMaximoRetrato = 512 * 1024;
        private const int LimiteDeParedes = 2000;

        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<CenaHub> _hub;

        public CenaConteudoController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser, IHubContext<CenaHub> hub)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
            _hub = hub;
        }

        private async Task<(PapelCampanha papel, Cena? cena, ActionResult? erro)> CarregarAsync(int campanhaId, int cenaId, bool exigeMestre)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return (default, null, NotFound("Campanha não encontrada."));
            if (exigeMestre && papel != PapelCampanha.Mestre)
                return (papel.Value, null, Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode mexer nos mapas."));

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            // para o jogador, uma cena que não está em jogo simplesmente não existe
            if (cena == null || (papel != PapelCampanha.Mestre && !cena.Ativa)) return (papel.Value, null, NotFound("Cena não encontrada."));
            return (papel.Value, cena, null);
        }

        // ---------- duplicar ----------

        /// <summary>Copia a cena com imagem, tokens (sem o vínculo ao combate), retratos, paredes e névoa. A cópia não entra em jogo.</summary>
        [HttpPost("duplicar")]
        [EnableRateLimiting("upload")]
        public async Task<ActionResult<CenaDto>> Duplicar(int campanhaId, int cenaId)
        {
            var (_, origem, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var imagem = await _uof.CenaRepository.GetImagemAsync(cenaId);
            if (imagem == null) return NotFound("Imagem não encontrada.");
            var tokens = await _uof.CenaRepository.GetTokensAsync(cenaId);
            var paredes = await _uof.CenaRepository.GetParedesAsync(cenaId);
            var existentes = await _uof.CenaRepository.GetPorCampanhaAsync(campanhaId);

            var nome = $"{origem!.Nome} (cópia)";
            var copia = new Cena
            {
                CampanhaId = campanhaId,
                Nome = nome.Length > 80 ? nome[..80] : nome,
                Largura = origem.Largura,
                Altura = origem.Altura,
                TamanhoCelula = origem.TamanhoCelula,
                Ativa = false,
                Ordem = existentes.Max(c => c.Ordem) + 1,
                NeblinaAtiva = origem.NeblinaAtiva,
                SessaoId = origem.SessaoId,
                SoNoTurno = origem.SoNoTurno,
                MetrosPorQuadrado = origem.MetrosPorQuadrado,
                LimitarDeslocamento = origem.LimitarDeslocamento,
            };
            await _uof.CenaRepository.AddAsync(copia, new CenaImagem
            {
                Tipo = imagem.Tipo, Dados = imagem.Dados, Neblina = imagem.Neblina?.ToArray(),
            });
            await _uof.CommitAsync(); // precisa do id da cena para pendurar o resto

            var novosTokens = tokens.Select(t => new TokenCena
            {
                CenaId = copia.CenaId, Nome = t.Nome, Coluna = t.Coluna, Linha = t.Linha, Tamanho = t.Tamanho, Rotacao = t.Rotacao, Cor = t.Cor,
                Inimigo = t.Inimigo, Oculto = t.Oculto, Visao = t.Visao, ImagemVersao = t.ImagemVersao, DeslocamentoMetros = t.DeslocamentoMetros,
                PersonagemId = t.PersonagemId, ControladoPorId = t.ControladoPorId, ModeloId = t.ModeloId,
                // o combatente não vai junto: o combate pertence à cena em que aconteceu
            }).ToList();
            await _uof.CenaRepository.AddTokensAsync(novosTokens);
            await _uof.CenaRepository.AddParedesAsync(paredes.Select(p => new ParedeCena
            {
                CenaId = copia.CenaId, X1 = p.X1, Y1 = p.Y1, X2 = p.X2, Y2 = p.Y2, Porta = p.Porta, Aberta = p.Aberta, Janela = p.Janela,
            }));
            await _uof.CenaRepository.AddMarcadoresAsync((await _uof.CenaRepository.GetMarcadoresAsync(cenaId)).Select(m => new MarcadorCena
            {
                CenaId = copia.CenaId, Coluna = m.Coluna, Linha = m.Linha, Icone = m.Icone, Titulo = m.Titulo, Nota = m.Nota, Visivel = m.Visivel,
            }));
            foreach (var o in await _uof.Modelo3dRepository.ListarObjetosAsync(cenaId))
                await _uof.Modelo3dRepository.AddObjetoAsync(new ObjetoCena
                {
                    CenaId = copia.CenaId, ModeloId = o.ModeloId, Coluna = o.Coluna, Linha = o.Linha, Tamanho = o.Tamanho, Rotacao = o.Rotacao, Oculto = o.Oculto,
                    X = o.X, Y = o.Y, Z = o.Z, EscalaX = o.EscalaX, EscalaY = o.EscalaY, EscalaZ = o.EscalaZ, GiroX = o.GiroX, GiroZ = o.GiroZ,
                });
            await _uof.CommitAsync();

            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].ImagemVersao == 0) continue;
                var retrato = await _uof.CenaRepository.GetTokenImagemAsync(tokens[i].TokenId);
                if (retrato != null)
                    await _uof.CenaRepository.AddTokenImagemAsync(new TokenImagem { TokenId = novosTokens[i].TokenId, Tipo = retrato.Tipo, Dados = retrato.Dados });
            }
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("CenaAtualizada");
            return Ok(CenaDto.De(copia));
        }

        // ---------- paredes e portas ----------

        [HttpGet("paredes")]
        public async Task<ActionResult<IEnumerable<ParedeDto>>> Paredes(int campanhaId, int cenaId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;

            return Ok((await _uof.CenaRepository.GetParedesAsync(cenaId)).Select(ParedeDto.De));
        }

        [HttpPost("paredes")]
        public async Task<ActionResult<ParedeDto>> CriarParede(int campanhaId, int cenaId, ParedeFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var invalido = Validar(cena!, form);
            if (invalido != null) return invalido;
            if (await _uof.CenaRepository.ContarParedesAsync(cenaId) >= LimiteDeParedes)
                return BadRequest(new { message = $"Uma cena aceita até {LimiteDeParedes} paredes." });

            var parede = new ParedeCena { CenaId = cenaId, X1 = form.X1, Y1 = form.Y1, X2 = form.X2, Y2 = form.Y2, Porta = form.Porta, Aberta = form.Porta && form.Aberta, Janela = form.Janela && !form.Porta };
            await _uof.CenaRepository.AddParedeAsync(parede);
            await _uof.CommitAsync();

            await AvisarParedesAsync(campanhaId, cenaId);
            return Ok(ParedeDto.De(parede));
        }

        /// <summary>Abrir e fechar uma porta (ou ajustar o segmento).</summary>
        [HttpPut("paredes/{paredeId:int}")]
        public async Task<ActionResult<ParedeDto>> EditarParede(int campanhaId, int cenaId, int paredeId, ParedeFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var parede = await _uof.CenaRepository.GetParedeAsync(cenaId, paredeId);
            if (parede == null) return NotFound("Parede não encontrada.");
            var invalido = Validar(cena!, form);
            if (invalido != null) return invalido;

            (parede.X1, parede.Y1, parede.X2, parede.Y2) = (form.X1, form.Y1, form.X2, form.Y2);
            parede.Porta = form.Porta;
            parede.Aberta = form.Porta && form.Aberta;
            parede.Janela = form.Janela && !form.Porta;
            await _uof.CommitAsync();

            await AvisarParedesAsync(campanhaId, cenaId);
            return Ok(ParedeDto.De(parede));
        }

        [HttpDelete("paredes/{paredeId:int}")]
        public async Task<IActionResult> ExcluirParede(int campanhaId, int cenaId, int paredeId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var parede = await _uof.CenaRepository.GetParedeAsync(cenaId, paredeId);
            if (parede == null) return NotFound("Parede não encontrada.");
            await _uof.CenaRepository.RemoveParedeAsync(parede);
            await _uof.CommitAsync();

            await AvisarParedesAsync(campanhaId, cenaId);
            return NoContent();
        }

        private ActionResult? Validar(Cena cena, ParedeFormDto form)
        {
            var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
            if (form.X1 == form.X2 && form.Y1 == form.Y2)
                return BadRequest(new { message = "A parede precisa ter comprimento." });
            if (form.X1 > colunas || form.X2 > colunas || form.Y1 > linhas || form.Y2 > linhas)
                return BadRequest(new { message = "A parede passa dos limites do mapa." });
            return null;
        }

        private Task AvisarParedesAsync(int campanhaId, int cenaId) =>
            _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("ParedesAtualizadas", new { cenaId });

        // ---------- marcadores ----------

        [HttpGet("marcadores")]
        public async Task<ActionResult<IEnumerable<MarcadorDto>>> Marcadores(int campanhaId, int cenaId)
        {
            var (papel, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;

            var todos = await _uof.CenaRepository.GetMarcadoresAsync(cenaId);
            return Ok(todos.Where(m => papel == PapelCampanha.Mestre || m.Visivel).Select(MarcadorDto.De));
        }

        [HttpPost("marcadores")]
        public async Task<ActionResult<MarcadorDto>> CriarMarcador(int campanhaId, int cenaId, MarcadorFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;
            if (!IconesDeMarcador.Valido(form.Icone))
                return BadRequest(new { message = $"Ícone inválido. Use: {string.Join(", ", IconesDeMarcador.Todos)}." });

            var marcador = new MarcadorCena { CenaId = cenaId };
            PreencherMarcador(marcador, form, cena!);
            await _uof.CenaRepository.AddMarcadorAsync(marcador);
            await _uof.CommitAsync();

            await AvisarMarcadorAsync(campanhaId, marcador);
            return Ok(MarcadorDto.De(marcador));
        }

        [HttpPut("marcadores/{marcadorId:int}")]
        public async Task<ActionResult<MarcadorDto>> EditarMarcador(int campanhaId, int cenaId, int marcadorId, MarcadorFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;
            if (!IconesDeMarcador.Valido(form.Icone))
                return BadRequest(new { message = $"Ícone inválido. Use: {string.Join(", ", IconesDeMarcador.Todos)}." });

            var marcador = await _uof.CenaRepository.GetMarcadorAsync(cenaId, marcadorId);
            if (marcador == null) return NotFound("Marcador não encontrado.");

            var eraVisivel = marcador.Visivel;
            PreencherMarcador(marcador, form, cena!);
            await _uof.CommitAsync();

            // escondido agora: os jogadores perdem o marcador antes de o Mestre receber a versão nova
            if (eraVisivel && !marcador.Visivel)
                await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("MarcadorRemovido", new { marcadorId, cenaId });
            await AvisarMarcadorAsync(campanhaId, marcador);
            return Ok(MarcadorDto.De(marcador));
        }

        [HttpDelete("marcadores/{marcadorId:int}")]
        public async Task<IActionResult> ExcluirMarcador(int campanhaId, int cenaId, int marcadorId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var marcador = await _uof.CenaRepository.GetMarcadorAsync(cenaId, marcadorId);
            if (marcador == null) return NotFound("Marcador não encontrado.");
            await _uof.CenaRepository.RemoveMarcadorAsync(marcador);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("MarcadorRemovido", new { marcadorId, cenaId });
            return NoContent();
        }

        private static void PreencherMarcador(MarcadorCena marcador, MarcadorFormDto form, Cena cena)
        {
            var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
            marcador.Coluna = Math.Clamp(form.Coluna, 0, colunas - 1);
            marcador.Linha = Math.Clamp(form.Linha, 0, linhas - 1);
            marcador.Icone = form.Icone;
            marcador.Titulo = form.Titulo.Trim();
            marcador.Nota = form.Nota?.Trim() ?? string.Empty;
            marcador.Visivel = form.Visivel;
        }

        private Task AvisarMarcadorAsync(int campanhaId, MarcadorCena marcador) =>
            _hub.Clients.Group(marcador.Visivel ? CenaHub.GrupoDaMesa(campanhaId) : CenaHub.GrupoDoMestre(campanhaId))
                .SendAsync("MarcadorAtualizado", MarcadorDto.De(marcador));

        // ---------- desenho livre ----------

        private const int LimiteDeTracos = 500;
        private const int LimiteDePontos = 600;

        [HttpGet("tracos")]
        public async Task<ActionResult<IEnumerable<TracoDto>>> Tracos(int campanhaId, int cenaId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;

            return Ok((await _uof.CenaRepository.GetTracosAsync(cenaId)).Select(TracoDto.De));
        }

        /// <summary>Qualquer membro desenha (o jogador, só na cena em jogo).</summary>
        [HttpPost("tracos")]
        public async Task<ActionResult<TracoDto>> CriarTraco(int campanhaId, int cenaId, TracoFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;
            if (!PaletaDeTokens.Valida(form.Cor))
                return BadRequest(new { message = $"Cor inválida. Use: {string.Join(", ", PaletaDeTokens.Cores)}." });
            if (form.Pontos is not { Count: >= 2 and <= LimiteDePontos } pontos
                || pontos.Any(p => p.Length != 2 || !double.IsFinite(p[0]) || !double.IsFinite(p[1]) || p[0] < 0 || p[1] < 0 || p[0] > 20000 || p[1] > 20000))
                return BadRequest(new { message = $"O traço precisa de 2 a {LimiteDePontos} pontos dentro do mapa." });
            if (await _uof.CenaRepository.ContarTracosAsync(cenaId) >= LimiteDeTracos)
                return BadRequest(new { message = $"Uma cena aceita até {LimiteDeTracos} traços. Peça ao Mestre para limpar." });

            var traco = new TracoCena
            {
                CenaId = cenaId, UsuarioId = _currentUser.UserId, Cor = form.Cor, Espessura = form.Espessura,
                Pontos = TracoDto.EscreverPontos(pontos),
            };
            await _uof.CenaRepository.AddTracoAsync(traco);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("TracoCriado", TracoDto.De(traco));
            return Ok(TracoDto.De(traco));
        }

        /// <summary>Cada um apaga o que desenhou; o Mestre apaga qualquer traço.</summary>
        [HttpDelete("tracos/{tracoId:int}")]
        public async Task<IActionResult> ExcluirTraco(int campanhaId, int cenaId, int tracoId)
        {
            var (papel, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;

            var traco = await _uof.CenaRepository.GetTracoAsync(cenaId, tracoId);
            if (traco == null) return NotFound("Traço não encontrado.");
            if (papel != PapelCampanha.Mestre && traco.UsuarioId != _currentUser.UserId)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Você só apaga o que desenhou.");

            await _uof.CenaRepository.RemoveTracoAsync(traco);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("TracoRemovido", new { tracoId, cenaId });
            return NoContent();
        }

        [HttpDelete("tracos")]
        public async Task<IActionResult> LimparTracos(int campanhaId, int cenaId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            await _uof.CenaRepository.RemoveTodosOsTracosAsync(cenaId);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("TracosLimpos", new { cenaId });
            return NoContent();
        }

        // ---------- retrato do token ----------

        [HttpGet("tokens/{tokenId:int}/imagem")]
        public async Task<IActionResult> Retrato(int campanhaId, int cenaId, int tokenId)
        {
            var (papel, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;

            var token = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            if (token == null || token.ImagemVersao == 0 || (papel != PapelCampanha.Mestre && token.Oculto))
                return NotFound("Imagem não encontrada.");

            var etag = $"\"t{tokenId}-{token.ImagemVersao}\"";
            if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

            var imagem = await _uof.CenaRepository.GetTokenImagemAsync(tokenId);
            if (imagem == null) return NotFound("Imagem não encontrada.");
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, no-cache";
            return File(imagem.Dados, imagem.Tipo);
        }

        [HttpPut("tokens/{tokenId:int}/imagem")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanhoMaximoRetrato + 64 * 1024)]
        public async Task<ActionResult<TokenCenaDto>> TrocarRetrato(int campanhaId, int cenaId, int tokenId, [FromForm] TokenImagemFormDto form)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var token = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            if (token == null) return NotFound("Token não encontrado.");

            var arquivo = form.Imagem!;
            if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximoRetrato)
                return BadRequest(new { message = "O retrato precisa ter até 512 KB." });
            byte[] bytes;
            using (var ms = new MemoryStream((int)arquivo.Length))
            {
                await arquivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            var tipo = TiposDeImagem.Identificar(bytes);
            if (tipo == null) return BadRequest(new { message = "Use uma imagem PNG, JPG ou WebP." });

            var atual = await _uof.CenaRepository.GetTokenImagemParaEditarAsync(tokenId);
            if (atual == null) await _uof.CenaRepository.AddTokenImagemAsync(new TokenImagem { TokenId = tokenId, Tipo = tipo, Dados = bytes });
            else (atual.Tipo, atual.Dados) = (tipo, bytes);
            token.ImagemVersao++;
            await _uof.CommitAsync();

            await AvisarTokenAsync(campanhaId, token);
            return Ok(TokenCenaDto.De(token));
        }

        [HttpDelete("tokens/{tokenId:int}/imagem")]
        public async Task<ActionResult<TokenCenaDto>> RemoverRetrato(int campanhaId, int cenaId, int tokenId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;

            var token = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            if (token == null) return NotFound("Token não encontrado.");

            var atual = await _uof.CenaRepository.GetTokenImagemParaEditarAsync(tokenId);
            if (atual != null) await _uof.CenaRepository.RemoveTokenImagemAsync(atual);
            token.ImagemVersao = 0;
            await _uof.CommitAsync();

            await AvisarTokenAsync(campanhaId, token);
            return Ok(TokenCenaDto.De(token));
        }

        private Task AvisarTokenAsync(int campanhaId, TokenCena token) =>
            _hub.Clients.Group(token.Oculto ? CenaHub.GrupoDoMestre(campanhaId) : CenaHub.GrupoDaMesa(campanhaId))
                .SendAsync("TokenAtualizado", TokenCenaDto.De(token));
    }
}
