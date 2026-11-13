using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Wyvern.Api.Hubs;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;
using CenaEntity = Wyvern.Domain.Entities.Cena;

namespace Wyvern.Api.Controllers
{
    /// <summary>
    /// Biblioteca de modelos 3D (.glb) da campanha. Todo membro vê e baixa. O Mestre sobe peças de cenário e miniaturas;
    /// o jogador sobe miniaturas (para o próprio herói). Apaga quem subiu, ou o Mestre.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Modelos")]
    public class Modelo3dController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<CenaHub> _hub;
        private readonly Modelos3dOptions _limites;

        public Modelo3dController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser, IHubContext<CenaHub> hub,
            IOptions<Modelos3dOptions> limites)
        {
            _limites = limites.Value;
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
            _hub = hub;
        }

        [HttpGet]
        public async Task<ActionResult<BibliotecaDeModelosDto>> Listar(int campanhaId)
        {
            if (await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId) == null) return NotFound("Campanha não encontrada.");

            var modelos = await _uof.Modelo3dRepository.ListarAsync(campanhaId);
            return Ok(new BibliotecaDeModelosDto
            {
                Modelos = modelos.Select(Modelo3dDto.De).ToList(),
                EspacoUsado = modelos.Sum(m => m.Tamanho),
                EspacoTotal = _limites.EspacoPorCampanha,
                TamanhoMaximoArquivo = _limites.TamanhoMaximo,
            });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(ArquivoGlb.TetoDaRequisicao)]
        [RequestFormLimits(MultipartBodyLengthLimit = ArquivoGlb.TetoDaRequisicao)]
        public async Task<ActionResult<Modelo3dDto>> Enviar(int campanhaId, [FromForm] Modelo3dFormDto form)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return NotFound("Campanha não encontrada.");
            if (!ArquivoGlb.CategoriaValida(form.Categoria))
                return BadRequest(new { message = "Categoria inválida. Use \"Cenario\" ou \"Miniatura\"." });
            var escala = string.IsNullOrWhiteSpace(form.Escala) ? Modelo3d.EscalaAuto : form.Escala;
            if (!ArquivoGlb.EscalaValida(escala))
                return BadRequest(new { message = "Escala inválida. Use \"Auto\", \"Impressao\", \"Real\" ou \"Encaixar\"." });
            if (papel != PapelCampanha.Mestre && form.Categoria != Modelo3d.Miniatura)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só o Mestre sobe peças de cenário. Você pode subir a miniatura do seu herói.");

            var arquivo = form.Arquivo!;
            if (arquivo.Length == 0 || arquivo.Length > _limites.TamanhoMaximo)
                return BadRequest(new { message = $"O modelo precisa ter até {_limites.TamanhoMaximo / (1024 * 1024)} MB." });
            var usado = await _uof.Modelo3dRepository.EspacoUsadoAsync(campanhaId);
            if (usado + arquivo.Length > _limites.EspacoPorCampanha)
                return BadRequest(new { message = $"A biblioteca desta campanha está cheia ({_limites.EspacoPorCampanhaMb} MB). Apague modelos que não usa mais para subir outro." });

            byte[] bytes;
            using (var ms = new MemoryStream((int)arquivo.Length))
            {
                await arquivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            if (!ArquivoGlb.Valido(bytes))
                return BadRequest(new { message = "Use um arquivo .glb (glTF binário, versão 2). Exporte do Blender como \"glTF Binary\"." });

            var modelo = new Modelo3d
            {
                CampanhaId = campanhaId,
                Nome = form.Nome.Trim(),
                Categoria = form.Categoria,
                Escala = escala,
                Tamanho = bytes.Length,
                EnviadoPorId = _currentUser.UserId,
                CriadoEm = DateTime.UtcNow,
            };
            await _uof.Modelo3dRepository.AddAsync(modelo, bytes);
            await _uof.CommitAsync();

            var salvo = await _uof.Modelo3dRepository.GetAsync(campanhaId, modelo.ModeloId);
            await AvisarAsync(campanhaId);
            return Ok(Modelo3dDto.De(salvo!));
        }

        /// <summary>O .glb. Os bytes de um modelo nunca mudam (trocar = subir outro), então o navegador pode guardá-lo.</summary>
        [HttpGet("{modeloId:int}/arquivo")]
        public async Task<IActionResult> Arquivo(int campanhaId, int modeloId)
        {
            if (await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId) == null) return NotFound("Campanha não encontrada.");
            if (await _uof.Modelo3dRepository.GetAsync(campanhaId, modeloId) == null) return NotFound("Modelo não encontrado.");

            var etag = $"\"m{modeloId}\"";
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

            var dados = await _uof.Modelo3dRepository.GetDadosAsync(modeloId);
            if (dados == null) return NotFound("Modelo não encontrado.");
            return File(dados, "model/gltf-binary");
        }

        /// <summary>Renomeia o modelo ou troca a escala (como as medidas do arquivo viram quadrados). Quem subiu, ou o Mestre.</summary>
        [HttpPut("{modeloId:int}")]
        public async Task<ActionResult<Modelo3dDto>> Editar(int campanhaId, int modeloId, Modelo3dEdicaoDto dto)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return NotFound("Campanha não encontrada.");
            var modelo = await _uof.Modelo3dRepository.GetAsync(campanhaId, modeloId);
            if (modelo == null) return NotFound("Modelo não encontrado.");
            if (papel != PapelCampanha.Mestre && modelo.EnviadoPorId != _currentUser.UserId)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só quem subiu o modelo, ou o Mestre, pode alterá-lo.");
            if (!ArquivoGlb.EscalaValida(dto.Escala))
                return BadRequest(new { message = "Escala inválida. Use \"Auto\", \"Impressao\", \"Real\" ou \"Encaixar\"." });

            modelo.Nome = dto.Nome.Trim();
            modelo.Escala = dto.Escala;
            await _uof.CommitAsync();

            await AvisarAsync(campanhaId);
            return Ok(Modelo3dDto.De(modelo));
        }

        /// <summary>
        /// Troca o arquivo de um modelo por outro (a versão otimizada que o navegador gera). Vira um modelo novo, com o
        /// mesmo nome, categoria, escala e autor, e tudo o que usava o antigo passa a usar ele: os bytes de um id nunca
        /// mudam, então quem guardou o arquivo antigo no cache não fica com ele. Quem subiu, ou o Mestre.
        /// </summary>
        [HttpPost("{modeloId:int}/substituir")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(ArquivoGlb.TetoDaRequisicao)]
        [RequestFormLimits(MultipartBodyLengthLimit = ArquivoGlb.TetoDaRequisicao)]
        public async Task<ActionResult<Modelo3dDto>> Substituir(int campanhaId, int modeloId, [FromForm] Modelo3dSubstituicaoDto form)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return NotFound("Campanha não encontrada.");
            var antigo = await _uof.Modelo3dRepository.GetAsync(campanhaId, modeloId);
            if (antigo == null) return NotFound("Modelo não encontrado.");
            if (papel != PapelCampanha.Mestre && antigo.EnviadoPorId != _currentUser.UserId)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só quem subiu o modelo, ou o Mestre, pode alterá-lo.");

            var arquivo = form.Arquivo!;
            if (arquivo.Length == 0 || arquivo.Length > _limites.TamanhoMaximo)
                return BadRequest(new { message = $"O modelo precisa ter até {_limites.TamanhoMaximo / (1024 * 1024)} MB." });
            var usado = await _uof.Modelo3dRepository.EspacoUsadoAsync(campanhaId);
            if (usado - antigo.Tamanho + arquivo.Length > _limites.EspacoPorCampanha)
                return BadRequest(new { message = $"A biblioteca desta campanha está cheia ({_limites.EspacoPorCampanhaMb} MB)." });

            byte[] bytes;
            using (var ms = new MemoryStream((int)arquivo.Length))
            {
                await arquivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            if (!ArquivoGlb.Valido(bytes))
                return BadRequest(new { message = "Use um arquivo .glb (glTF binário, versão 2)." });

            var novo = new Modelo3d
            {
                CampanhaId = campanhaId,
                Nome = antigo.Nome,
                Categoria = antigo.Categoria,
                Escala = antigo.Escala,
                Tamanho = bytes.Length,
                EnviadoPorId = antigo.EnviadoPorId,
                CriadoEm = antigo.CriadoEm,
            };
            await _uof.Modelo3dRepository.SubstituirAsync(antigo, novo, bytes);
            await _uof.CommitAsync();

            var salvo = await _uof.Modelo3dRepository.GetAsync(campanhaId, novo.ModeloId);
            await AvisarAsync(campanhaId);
            return Ok(Modelo3dDto.De(salvo!));
        }

        /// <summary>Apaga o modelo: as peças com ele saem dos mapas e quem o usava como miniatura volta ao token comum.</summary>
        [HttpDelete("{modeloId:int}")]
        public async Task<IActionResult> Apagar(int campanhaId, int modeloId)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return NotFound("Campanha não encontrada.");
            var modelo = await _uof.Modelo3dRepository.GetAsync(campanhaId, modeloId);
            if (modelo == null) return NotFound("Modelo não encontrado.");
            if (papel != PapelCampanha.Mestre && modelo.EnviadoPorId != _currentUser.UserId)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só quem subiu o modelo, ou o Mestre, pode apagá-lo.");

            _uof.Modelo3dRepository.Remover(modelo);
            await _uof.CommitAsync();

            await AvisarAsync(campanhaId);
            return NoContent();
        }

        /// <summary>A biblioteca, as peças nos mapas ou as miniaturas mudaram: a mesa recarrega o que usa modelos.</summary>
        private Task AvisarAsync(int campanhaId) =>
            _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("ModelosAtualizados");
    }

    /// <summary>Peças de cenário postas num mapa. Membros veem (jogador: só as visíveis da cena em jogo); só o Mestre mexe.</summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Cenas/{cenaId:int}/objetos")]
    public class ObjetoCenaController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<CenaHub> _hub;

        public ObjetoCenaController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser, IHubContext<CenaHub> hub)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
            _hub = hub;
        }

        private async Task<(PapelCampanha papel, CenaEntity? cena, ActionResult? erro)> CarregarAsync(int campanhaId, int cenaId, bool exigeMestre)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return (default, null, NotFound("Campanha não encontrada."));
            if (exigeMestre && papel != PapelCampanha.Mestre)
                return (papel.Value, null, Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Apenas o Mestre pode mexer nos mapas."));
            var cena = await _uof.CenaRepository.GetAsync(campanhaId, cenaId);
            if (cena == null || (papel != PapelCampanha.Mestre && !cena.Ativa)) return (papel.Value, null, NotFound("Cena não encontrada."));
            return (papel.Value, cena, null);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ObjetoCenaDto>>> Listar(int campanhaId, int cenaId)
        {
            var (papel, _, erro) = await CarregarAsync(campanhaId, cenaId, false);
            if (erro != null) return erro;
            var objetos = await _uof.Modelo3dRepository.ListarObjetosAsync(cenaId);
            return Ok(objetos.Where(o => papel == PapelCampanha.Mestre || !o.Oculto).Select(ObjetoCenaDto.De));
        }

        [HttpPost]
        public async Task<ActionResult<ObjetoCenaDto>> Criar(int campanhaId, int cenaId, ObjetoCenaFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;
            var invalido = await ValidarAsync(campanhaId, form);
            if (invalido != null) return invalido;
            if (await _uof.Modelo3dRepository.ContarObjetosAsync(cenaId) >= ArquivoGlb.ObjetosPorCena)
                return BadRequest(new { message = $"Um mapa aceita até {ArquivoGlb.ObjetosPorCena} peças de cenário." });

            var objeto = new ObjetoCena { CenaId = cenaId };
            Preencher(objeto, form, cena!);
            await _uof.Modelo3dRepository.AddObjetoAsync(objeto);
            await _uof.CommitAsync();

            var salvo = await _uof.Modelo3dRepository.GetObjetoAsync(cenaId, objeto.ObjetoId);
            await AvisarAsync(campanhaId, salvo!);
            return Ok(ObjetoCenaDto.De(salvo!));
        }

        [HttpPut("{objetoId:int}")]
        public async Task<ActionResult<ObjetoCenaDto>> Editar(int campanhaId, int cenaId, int objetoId, ObjetoCenaFormDto form)
        {
            var (_, cena, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;
            var objeto = await _uof.Modelo3dRepository.GetObjetoAsync(cenaId, objetoId);
            if (objeto == null) return NotFound("Peça não encontrada.");
            var invalido = await ValidarAsync(campanhaId, form);
            if (invalido != null) return invalido;

            var eraVisivel = !objeto.Oculto;
            Preencher(objeto, form, cena!);
            await _uof.CommitAsync();

            var salvo = await _uof.Modelo3dRepository.GetObjetoAsync(cenaId, objetoId);
            // escondida agora: os jogadores perdem a peça antes de o Mestre receber a versão nova
            if (eraVisivel && salvo!.Oculto)
                await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("ObjetoRemovido", new { objetoId, cenaId });
            await AvisarAsync(campanhaId, salvo!);
            return Ok(ObjetoCenaDto.De(salvo!));
        }

        [HttpDelete("{objetoId:int}")]
        public async Task<IActionResult> Apagar(int campanhaId, int cenaId, int objetoId)
        {
            var (_, _, erro) = await CarregarAsync(campanhaId, cenaId, true);
            if (erro != null) return erro;
            var objeto = await _uof.Modelo3dRepository.GetObjetoAsync(cenaId, objetoId);
            if (objeto == null) return NotFound("Peça não encontrada.");

            _uof.Modelo3dRepository.RemoverObjeto(objeto);
            await _uof.CommitAsync();

            await _hub.Clients.Group(CenaHub.GrupoDaMesa(campanhaId)).SendAsync("ObjetoRemovido", new { objetoId, cenaId });
            return NoContent();
        }

        private async Task<ActionResult?> ValidarAsync(int campanhaId, ObjetoCenaFormDto form)
        {
            var modelo = await _uof.Modelo3dRepository.GetAsync(campanhaId, form.ModeloId);
            if (modelo == null || modelo.Categoria != Modelo3d.Cenario)
                return BadRequest(new { message = "Escolha uma peça de cenário da biblioteca desta campanha." });
            return null;
        }

        private static void Preencher(ObjetoCena objeto, ObjetoCenaFormDto form, CenaEntity cena)
        {
            objeto.ModeloId = form.ModeloId;
            objeto.Oculto = form.Oculto;
            objeto.Rotacao = ((form.Rotacao % 360) + 360) % 360;
            objeto.GiroX = Inclinacao(form.GiroX ?? 0);
            objeto.GiroZ = Inclinacao(form.GiroZ ?? 0);

            if (form.X is { } x && form.Z is { } z)
            {
                // editor 3D: posição livre, dentro do mapa
                var colunas = (double)cena.Largura / Math.Max(1, cena.TamanhoCelula);
                var linhas = (double)cena.Altura / Math.Max(1, cena.TamanhoCelula);
                objeto.X = Math.Clamp(Finito(x), 0, colunas);
                objeto.Z = Math.Clamp(Finito(z), 0, linhas);
                objeto.Y = Math.Clamp(Finito(form.Y ?? 0), ObjetoCenaRegras.AlturaMinima, ObjetoCenaRegras.AlturaMaxima);
                objeto.EscalaX = Escala(form.EscalaX ?? form.Tamanho);
                objeto.EscalaY = Escala(form.EscalaY ?? form.Tamanho);
                objeto.EscalaZ = Escala(form.EscalaZ ?? form.Tamanho);
            }
            else
            {
                // forma antiga (o mapa 2D): canto e lado em quadrados inteiros
                var (coluna, linha) = CenaRegras.Limitar(cena, form.Tamanho, form.Coluna, form.Linha);
                objeto.X = coluna + form.Tamanho / 2.0;
                objeto.Z = linha + form.Tamanho / 2.0;
                objeto.Y = form.Y is { } y ? Math.Clamp(Finito(y), ObjetoCenaRegras.AlturaMinima, ObjetoCenaRegras.AlturaMaxima) : objeto.Y;
                objeto.EscalaX = objeto.EscalaY = objeto.EscalaZ = form.Tamanho;
            }

            // o 2D e a névoa olham a peça pelos quadrados: o lado maior arredondado, em volta do centro
            objeto.Tamanho = Math.Clamp((int)Math.Round(Math.Max(objeto.EscalaX, objeto.EscalaZ)), 1, 10);
            (objeto.Coluna, objeto.Linha) = CenaRegras.Limitar(cena, objeto.Tamanho,
                (int)Math.Round(objeto.X - objeto.Tamanho / 2.0), (int)Math.Round(objeto.Z - objeto.Tamanho / 2.0));
        }

        private static double Finito(double v) => double.IsFinite(v) ? v : 0;
        private static double Escala(double v) => Math.Clamp(Finito(v), ObjetoCenaRegras.EscalaMinima, ObjetoCenaRegras.EscalaMaxima);
        private static double Inclinacao(double graus) => Math.IEEERemainder(Finito(graus), 360);

        private Task AvisarAsync(int campanhaId, ObjetoCena objeto) =>
            _hub.Clients.Group(objeto.Oculto ? CenaHub.GrupoDoMestre(campanhaId) : CenaHub.GrupoDaMesa(campanhaId))
                .SendAsync("ObjetoAtualizado", ObjetoCenaDto.De(objeto));
    }
}
