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
    /// <summary>
    /// Os mapas táticos da campanha (VTT). Membros veem a cena em jogo; o Mestre cria, edita e posiciona tudo.
    /// Quem não participa da campanha não sabe que ela existe (404). Mover tokens em tempo real é com o CenaHub.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Cenas")]
    public class CenaController : ControllerBase
    {
        private const long TamanhoMaximoImagem = 8 * 1024 * 1024;

        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<CenaHub> _hub;

        public CenaController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser, IHubContext<CenaHub> hub)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
            _hub = hub;
        }

        private Task<PapelCampanha?> PapelAsync(int campanhaId) => _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);

        private ActionResult Proibido(string detalhe) => Problem(statusCode: StatusCodes.Status403Forbidden, detail: detalhe);

        /// <summary>Papel do usuário, ou 404 se ele não participa. Com exigeMestre, jogador recebe 403.</summary>
        private async Task<(PapelCampanha papel, ActionResult? erro)> AcessoAsync(int campanhaId, bool exigeMestre)
        {
            var papel = await PapelAsync(campanhaId);
            if (papel == null) return (default, NotFound("Campanha não encontrada."));
            if (exigeMestre && papel != PapelCampanha.Mestre) return (papel.Value, Proibido("Apenas o Mestre pode mexer nos mapas."));
            return (papel.Value, null);
        }

        // ---------- cenas ----------

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CenaDto>>> Listar(int campanhaId)
        {
            var (papel, erro) = await AcessoAsync(campanhaId, false);
            if (erro != null) return erro;

            var cenas = await _uof.CenaRepository.GetPorCampanhaAsync(campanhaId);
            return Ok(cenas.Where(c => papel == PapelCampanha.Mestre || c.Ativa).Select(CenaDto.De));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanhoMaximoImagem + 64 * 1024)]
        [EnableRateLimiting("upload")]
        public async Task<ActionResult<CenaDto>> Criar(int campanhaId, [FromForm] CenaFormDto form)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var imagem = form.Imagem!;
            if (imagem.Length == 0 || imagem.Length > TamanhoMaximoImagem)
                return BadRequest(new { message = "A imagem do mapa precisa ter até 8 MB." });

            byte[] bytes;
            using (var ms = new MemoryStream((int)imagem.Length))
            {
                await imagem.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            var tipo = TipoDaImagem(bytes);
            if (tipo == null) return BadRequest(new { message = "Use uma imagem PNG, JPG ou WebP." });

            if (form.SessaoId is { } sessaoId && await _uof.SessaoRepository.GetCampanhaIdAsync(sessaoId) != campanhaId)
                return BadRequest(new { message = "A sessão informada não é desta campanha." });

            var existentes = await _uof.CenaRepository.GetPorCampanhaAsync(campanhaId);
            var cena = new Cena
            {
                SessaoId = form.SessaoId,
                CampanhaId = campanhaId,
                Nome = form.Nome.Trim(),
                Largura = form.Largura,
                Altura = form.Altura,
                TamanhoCelula = form.TamanhoCelula,
                Ordem = (existentes.Count == 0 ? 0 : existentes.Max(c => c.Ordem)) + 1,
                // a primeira cena já vai para a mesa: não deixa os jogadores olhando para o nada
                Ativa = existentes.Count == 0,
            };
            await _uof.CenaRepository.AddAsync(cena, new CenaImagem { Tipo = tipo, Dados = bytes });
            await _uof.CommitAsync();

            await AvisarCenaAtualizadaAsync(campanhaId);
            return Ok(CenaDto.De(cena));
        }

        [HttpPut("{cenaId:int}")]
        public async Task<ActionResult<CenaDto>> Editar(int campanhaId, int cenaId, CenaEdicaoDto form)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null) return NotFound("Cena não encontrada.");

            if (form.SessaoId is { } sessaoDaCena && await _uof.SessaoRepository.GetCampanhaIdAsync(sessaoDaCena) != campanhaId)
                return BadRequest(new { message = "A sessão informada não é desta campanha." });

            cena.SessaoId = form.SessaoId;
            cena.SoNoTurno = form.SoNoTurno;
            cena.MetrosPorQuadrado = form.MetrosPorQuadrado;
            cena.LimitarDeslocamento = form.LimitarDeslocamento;
            var mudouAGrade = cena.TamanhoCelula != form.TamanhoCelula;
            cena.Nome = form.Nome.Trim();
            cena.TamanhoCelula = form.TamanhoCelula;
            if (mudouAGrade)
            {
                // os quadrados mudaram de lugar: a névoa antiga não vale mais
                var imagem = await _uof.CenaRepository.GetImagemParaEditarAsync(cenaId);
                if (imagem != null) imagem.Neblina = null;
            }
            await _uof.CommitAsync();

            await AvisarCenaAtualizadaAsync(campanhaId);
            return Ok(CenaDto.De(cena));
        }

        [HttpPost("{cenaId:int}/ativar")]
        public async Task<ActionResult<CenaDto>> Ativar(int campanhaId, int cenaId)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cenas = await _uof.CenaRepository.GetPorCampanhaAsync(campanhaId);
            if (cenas.All(c => c.CenaId != cenaId)) return NotFound("Cena não encontrada.");

            // GetPorCampanhaAsync não rastreia: busca de novo as que mudam
            foreach (var c in cenas.Where(c => c.Ativa || c.CenaId == cenaId))
            {
                var rastreada = await _uof.CenaRepository.GetAsync(campanhaId, c.CenaId);
                rastreada!.Ativa = c.CenaId == cenaId;
            }
            await _uof.CommitAsync();

            await AvisarCenaAtualizadaAsync(campanhaId);
            var ativa = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            return Ok(CenaDto.De(ativa!));
        }

        [HttpDelete("{cenaId:int}")]
        public async Task<IActionResult> Excluir(int campanhaId, int cenaId)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null) return NotFound("Cena não encontrada.");

            await _uof.CenaRepository.RemoveAsync(cena);
            await _uof.CommitAsync();

            await AvisarCenaAtualizadaAsync(campanhaId);
            return NoContent();
        }

        [HttpGet("{cenaId:int}/imagem")]
        public async Task<IActionResult> Imagem(int campanhaId, int cenaId)
        {
            var (papel, erro) = await AcessoAsync(campanhaId, false);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null || (papel != PapelCampanha.Mestre && !cena.Ativa)) return NotFound("Cena não encontrada.");

            var etag = $"\"{cena.CenaId}-{cena.ImagemVersao}\"";
            if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

            var imagem = await _uof.CenaRepository.GetImagemAsync(cenaId);
            if (imagem == null) return NotFound("Imagem não encontrada.");

            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, no-cache";
            return File(imagem.Dados, imagem.Tipo);
        }

        // ---------- névoa de guerra ----------

        [HttpGet("{cenaId:int}/neblina")]
        public async Task<ActionResult<NeblinaDto>> Neblina(int campanhaId, int cenaId)
        {
            var (papel, erro) = await AcessoAsync(campanhaId, false);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null || (papel != PapelCampanha.Mestre && !cena.Ativa)) return NotFound("Cena não encontrada.");

            var imagem = await _uof.CenaRepository.GetImagemAsync(cenaId);
            return Ok(NeblinaRegras.Montar(cena, imagem?.Neblina));
        }

        /// <summary>Liga ou desliga a névoa e, se pedido, cobre ou revela o mapa inteiro.</summary>
        [HttpPut("{cenaId:int}/neblina")]
        public async Task<ActionResult<NeblinaDto>> AjustarNeblina(int campanhaId, int cenaId, NeblinaAjusteDto ajuste)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null) return NotFound("Cena não encontrada.");
            if (ajuste.Tudo is not (null or "" or "revelar" or "cobrir"))
                return BadRequest(new { message = "Use \"revelar\" ou \"cobrir\"." });

            var imagem = await _uof.CenaRepository.GetImagemParaEditarAsync(cenaId);
            cena.NeblinaAtiva = ajuste.Ativa;
            if (imagem != null && !string.IsNullOrEmpty(ajuste.Tudo))
            {
                var (colunas, linhas) = NeblinaRegras.Dimensoes(cena);
                imagem.Neblina = NeblinaRegras.Tudo(colunas, linhas, ajuste.Tudo == "revelar");
            }
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("NeblinaAtualizada", new { cenaId });
            return Ok(NeblinaRegras.Montar(cena, imagem?.Neblina));
        }

        [HttpGet("{cenaId:int}/tokens")]
        public async Task<ActionResult<IEnumerable<TokenCenaDto>>> Tokens(int campanhaId, int cenaId)
        {
            var (papel, erro) = await AcessoAsync(campanhaId, false);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null || (papel != PapelCampanha.Mestre && !cena.Ativa)) return NotFound("Cena não encontrada.");

            var tokens = await _uof.CenaRepository.GetTokensAsync(cenaId);
            return Ok(tokens.Where(t => papel == PapelCampanha.Mestre || !t.Oculto).Select(TokenCenaDto.De));
        }

        [HttpPost("{cenaId:int}/tokens")]
        public async Task<ActionResult<TokenCenaDto>> CriarToken(int campanhaId, int cenaId, TokenFormDto form)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null) return NotFound("Cena não encontrada.");

            var invalido = await ValidarTokenAsync(campanhaId, form);
            if (invalido != null) return invalido;

            var token = new TokenCena { CenaId = cenaId };
            Preencher(token, form, cena);
            await _uof.CenaRepository.AddTokensAsync([token]);
            await _uof.CommitAsync();

            var salvo = await _uof.CenaRepository.GetTokenAsync(cenaId, token.TokenId);
            await AvisarTokenAtualizadoAsync(campanhaId, salvo!);
            return Ok(TokenCenaDto.De(salvo!));
        }

        [HttpPut("{cenaId:int}/tokens/{tokenId:int}")]
        public async Task<ActionResult<TokenCenaDto>> EditarToken(int campanhaId, int cenaId, int tokenId, TokenFormDto form)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var token = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            if (token?.Cena == null || token.Cena.CampanhaId != campanhaId) return NotFound("Token não encontrado.");

            var invalido = await ValidarTokenAsync(campanhaId, form);
            if (invalido != null) return invalido;

            var ficavaVisivel = !token.Oculto;
            Preencher(token, form, token.Cena);
            await _uof.CommitAsync();

            var salvo = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            // escondido agora: os jogadores perdem o token antes de o Mestre receber a versão nova
            if (ficavaVisivel && salvo!.Oculto)
                await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("TokenRemovido", new { tokenId, cenaId });
            await AvisarTokenAtualizadoAsync(campanhaId, salvo!);
            return Ok(TokenCenaDto.De(salvo!));
        }

        [HttpDelete("{cenaId:int}/tokens/{tokenId:int}")]
        public async Task<IActionResult> ExcluirToken(int campanhaId, int cenaId, int tokenId)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var token = await _uof.CenaRepository.GetTokenAsync(cenaId, tokenId);
            if (token?.Cena == null || token.Cena.CampanhaId != campanhaId) return NotFound("Token não encontrado.");

            await _uof.CenaRepository.RemoveTokenAsync(token);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("TokenRemovido", new { tokenId, cenaId });
            return NoContent();
        }

        /// <summary>Cria um token para cada combatente do combate ativo que ainda não está no mapa.</summary>
        [HttpPost("{cenaId:int}/tokens/do-combate")]
        public async Task<ActionResult<IEnumerable<TokenCenaDto>>> TokensDoCombate(int campanhaId, int cenaId, [FromQuery] int? sessaoId = null)
        {
            var (_, erro) = await AcessoAsync(campanhaId, true);
            if (erro != null) return erro;

            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null) return NotFound("Cena não encontrada.");

            // a campanha pode ter um combate ativo por sessão: vale o da sessão em que o Mestre está
            if (sessaoId is { } s && await _uof.SessaoRepository.GetCampanhaIdAsync(s) != campanhaId)
                return BadRequest(new { message = "A sessão informada não é desta campanha." });
            var combate = sessaoId is { } sessao
                ? await _uof.CombateRepository.GetActiveCombateBySessaoAsync(sessao)
                : await _uof.CombateRepository.GetActiveCombateByCampanhaAsync(campanhaId);
            if (combate?.Participantes == null || combate.Participantes.Count == 0)
                return BadRequest(new { message = "Não há combate em andamento." });

            var existentes = await _uof.CenaRepository.GetTokensAsync(cenaId);
            var jaNoMapa = existentes.Where(t => t.ParticipanteId != null).Select(t => t.ParticipanteId!.Value).ToHashSet();
            var ocupados = existentes.Select(t => (t.Coluna, t.Linha)).ToHashSet();

            var colunas = Math.Max(1, cena.Largura / cena.TamanhoCelula);
            var novos = new List<TokenCena>();
            var ligados = new List<int>();
            var proximo = 0;

            foreach (var p in Combate.Ordenar(combate.Participantes).Where(p => !jaNoMapa.Contains(p.ParticipanteId)))
            {
                // o personagem já está no mapa (colocado antes do combate): o token passa a representar o combatente
                var existente = p.PersonagemId is { } pid
                    ? existentes.FirstOrDefault(t => t.PersonagemId == pid && t.ParticipanteId == null && !ligados.Contains(t.TokenId))
                    : null;
                if (existente != null)
                {
                    var rastreado = await _uof.CenaRepository.GetTokenAsync(cenaId, existente.TokenId);
                    rastreado!.ParticipanteId = p.ParticipanteId;
                    ligados.Add(existente.TokenId);
                    continue;
                }

                // primeiro quadrado livre, da esquerda para a direita, de cima para baixo
                (int coluna, int linha) pos;
                do
                {
                    pos = (proximo % colunas, proximo / colunas);
                    proximo++;
                } while (ocupados.Contains(pos));
                ocupados.Add(pos);

                var nome = p.NomeNPC ?? p.Personagem?.Nome ?? "Combatente";
                var token = new TokenCena
                {
                    CenaId = cenaId,
                    Nome = nome.Length > 60 ? nome[..60] : nome,
                    Coluna = pos.coluna,
                    Linha = pos.linha,
                    Cor = p.IsInimigo ? "carmim" : "azul",
                    Inimigo = p.IsInimigo,
                    PersonagemId = p.PersonagemId,
                    ParticipanteId = p.ParticipanteId,
                };
                (token.Coluna, token.Linha) = CenaRegras.Limitar(cena, token.Tamanho, token.Coluna, token.Linha);
                novos.Add(token);
            }

            if (novos.Count > 0) await _uof.CenaRepository.AddTokensAsync(novos);
            if (novos.Count > 0 || ligados.Count > 0) await _uof.CommitAsync();

            // devolve (e anuncia) os tokens criados e também os que acabaram de ser ligados ao combate
            var salvos = (await _uof.CenaRepository.GetTokensAsync(cenaId))
                .Where(t => ligados.Contains(t.TokenId) || novos.Any(n => n.TokenId == t.TokenId)).ToList();
            foreach (var t in salvos) await AvisarTokenAtualizadoAsync(campanhaId, t);
            return Ok(salvos.Select(TokenCenaDto.De));
        }

        // ---------- apoio ----------

        private async Task<ActionResult?> ValidarTokenAsync(int campanhaId, TokenFormDto form)
        {
            if (!PaletaDeTokens.Valida(form.Cor))
                return BadRequest(new { message = $"Cor inválida. Use: {string.Join(", ", PaletaDeTokens.Cores)}." });
            if (form.PersonagemId is { } personagemId && !await _uof.CenaRepository.PersonagemEhDaCampanhaAsync(campanhaId, personagemId))
                return BadRequest(new { message = "O personagem informado não é desta campanha." });
            if (form.ParticipanteId is { } participanteId && !await _uof.CenaRepository.ParticipanteEhDaCampanhaAsync(campanhaId, participanteId))
                return BadRequest(new { message = "O combatente informado não é desta campanha." });
            if (form.ControladoPorId is { } usuarioId && !await _campanhaAuth.IsMembroAsync(campanhaId, usuarioId))
                return BadRequest(new { message = "Só quem participa da campanha pode controlar um token." });
            if (form.ModeloId is { } modeloId && (await _uof.Modelo3dRepository.GetAsync(campanhaId, modeloId))?.Categoria != Modelo3d.Miniatura)
                return BadRequest(new { message = "A miniatura precisa ser um modelo da categoria Miniaturas desta campanha." });
            return null;
        }

        private static void Preencher(TokenCena token, TokenFormDto form, Cena cena)
        {
            token.Nome = form.Nome.Trim();
            token.Tamanho = form.Tamanho;
            token.Cor = form.Cor;
            token.Inimigo = form.Inimigo;
            token.Oculto = form.Oculto;
            token.Visao = form.Visao;
            token.DeslocamentoMetros = form.DeslocamentoMetros;
            if (form.RestaurarMovimento)
            {
                token.MovimentoGasto = 0;
                token.MovimentoTurno = null;
            }
            token.PersonagemId = form.PersonagemId;
            token.ParticipanteId = form.ParticipanteId;
            token.ControladoPorId = form.ControladoPorId;
            token.ModeloId = form.ModeloId;
            (token.Coluna, token.Linha) = CenaRegras.Limitar(cena, token.Tamanho, form.Coluna, form.Linha);
        }

        /// <summary>Identifica PNG, JPEG e WebP pelos primeiros bytes (o tipo informado pelo cliente não vale).</summary>
        private static string? TipoDaImagem(byte[] b)
        {
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "image/webp";
            return null;
        }

        private Task AvisarCenaAtualizadaAsync(int campanhaId) =>
            _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("CenaAtualizada");

        /// <summary>Token novo ou alterado: os jogadores só recebem se ele estiver visível.</summary>
        private Task AvisarTokenAtualizadoAsync(int campanhaId, TokenCena token) =>
            _hub.Clients.Group(token.Oculto ? CenaHub.GrupoDoMestre(campanhaId) : CenaHub.GrupoDaMesa(campanhaId))
                .SendAsync("TokenAtualizado", TokenCenaDto.De(token));
    }
}
