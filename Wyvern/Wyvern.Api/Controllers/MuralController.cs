using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Cena;
using Wyvern.Application.DTOs.Mural;
using Wyvern.Application.Services;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces;
using Wyvern.Domain.Interfaces.Repositories;

namespace Wyvern.Api.Controllers
{
    public class MuralImagemFormDto
    {
        public IFormFile? Imagem { get; set; }
    }

    /// <summary>
    /// Murais: quadros livres do Mestre (estilo Miro). Só o Mestre da campanha vê e edita; para os jogadores eles
    /// não existem. O documento é salvo inteiro, com controle de versão para duas janelas não apagarem uma a outra.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("Campanha/{campanhaId:int}/Murais")]
    public class MuralController : ControllerBase
    {
        public const int TamanhoMaximoConteudo = 2 * 1024 * 1024;
        public const long TamanhoMaximoImagem = 5 * 1024 * 1024;
        public const int MaximoDeImagensPorMural = 200;
        /// <summary>Imagem recém-enviada ainda pode não estar no documento salvo: só some depois deste tempo sem uso.</summary>
        private static readonly TimeSpan CarenciaDeImagemSemUso = TimeSpan.FromMinutes(30);

        private readonly IUnitOfWork _uof;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICurrentUserService _currentUser;

        public MuralController(IUnitOfWork uof, ICampanhaAuthorizationService campanhaAuth, ICurrentUserService currentUser)
        {
            _uof = uof;
            _campanhaAuth = campanhaAuth;
            _currentUser = currentUser;
        }

        /// <summary>Quem não participa da campanha recebe 404; o jogador, 403: o mural é só do Mestre.</summary>
        private async Task<ActionResult?> SoMestreAsync(int campanhaId)
        {
            var papel = await _campanhaAuth.ObterPapelAsync(campanhaId, _currentUser.UserId);
            if (papel == null) return NotFound("Campanha não encontrada.");
            if (papel != PapelCampanha.Mestre)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Os murais são só do Mestre da campanha.");
            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MuralResumoDto>>> Listar(int campanhaId)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            var lista = await _uof.MuralRepository.ListarAsync(campanhaId);
            return Ok(lista.Select(m => new MuralResumoDto { MuralId = m.MuralId, Nome = m.Nome, AtualizadoEm = m.AtualizadoEm }));
        }

        [HttpPost]
        public async Task<ActionResult<MuralDto>> Criar(int campanhaId, MuralCriarDto dto)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;

            var agora = DateTime.UtcNow;
            var mural = new Mural
            {
                CampanhaId = campanhaId, Nome = dto.Nome.Trim(), Conteudo = "{}", Versao = 1,
                CriadoPorId = _currentUser.UserId, CriadoEm = agora, AtualizadoEm = agora,
            };
            await _uof.MuralRepository.AddAsync(mural);
            await _uof.CommitAsync();
            return CreatedAtAction(nameof(Obter), new { campanhaId, muralId = mural.MuralId }, MuralDto.De(mural));
        }

        [HttpGet("{muralId:int}")]
        public async Task<ActionResult<MuralDto>> Obter(int campanhaId, int muralId)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            var mural = await _uof.MuralRepository.GetAsync(campanhaId, muralId);
            return mural == null ? NotFound("Mural não encontrado.") : Ok(MuralDto.De(mural));
        }

        /// <summary>Grava o documento inteiro. Versão diferente da atual: 409, com a versão que está no servidor.</summary>
        [HttpPut("{muralId:int}")]
        [RequestSizeLimit(TamanhoMaximoConteudo + 64 * 1024)]
        public async Task<ActionResult<MuralSalvoDto>> Salvar(int campanhaId, int muralId, MuralSalvarDto dto)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            var mural = await _uof.MuralRepository.GetAsync(campanhaId, muralId);
            if (mural == null) return NotFound("Mural não encontrado.");

            if (dto.Versao != mural.Versao)
                return Conflict(new { message = "Este mural foi alterado em outra janela. Recarregue para ver a versão mais nova.", versao = mural.Versao });
            if (System.Text.Encoding.UTF8.GetByteCount(dto.Conteudo) > TamanhoMaximoConteudo)
                return BadRequest(new { message = "O mural ficou grande demais (o limite é 2 MB de conteúdo). Divida em mais de um mural." });

            HashSet<int> imagensEmUso;
            try
            {
                using var documento = JsonDocument.Parse(dto.Conteudo);
                if (documento.RootElement.ValueKind != JsonValueKind.Object)
                    return BadRequest(new { message = "Conteúdo do mural inválido." });
                imagensEmUso = ImagensUsadas(documento.RootElement);
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "Conteúdo do mural inválido." });
            }

            mural.Nome = dto.Nome.Trim();
            mural.Conteudo = dto.Conteudo;
            mural.Versao++;
            mural.AtualizadoEm = DateTime.UtcNow;
            await _uof.MuralRepository.RemoverImagensSemUsoAsync(muralId, imagensEmUso, DateTime.UtcNow - CarenciaDeImagemSemUso);
            await _uof.CommitAsync();
            return Ok(new MuralSalvoDto { Versao = mural.Versao, AtualizadoEm = mural.AtualizadoEm });
        }

        [HttpDelete("{muralId:int}")]
        public async Task<IActionResult> Excluir(int campanhaId, int muralId)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            var mural = await _uof.MuralRepository.GetAsync(campanhaId, muralId);
            if (mural == null) return NotFound("Mural não encontrado.");
            _uof.MuralRepository.Remover(mural);
            await _uof.CommitAsync();
            return NoContent();
        }

        // ---------- imagens ----------

        [HttpPost("{muralId:int}/imagens")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanhoMaximoImagem + 64 * 1024)]
        public async Task<ActionResult<MuralImagemCriadaDto>> EnviarImagem(int campanhaId, int muralId, [FromForm] MuralImagemFormDto form)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            if (await _uof.MuralRepository.GetAsync(campanhaId, muralId) == null) return NotFound("Mural não encontrado.");

            var arquivo = form.Imagem;
            if (arquivo == null || arquivo.Length == 0 || arquivo.Length > TamanhoMaximoImagem)
                return BadRequest(new { message = "A imagem precisa ter até 5 MB." });
            if (await _uof.MuralRepository.ContarImagensAsync(muralId) >= MaximoDeImagensPorMural)
                return BadRequest(new { message = $"Um mural guarda até {MaximoDeImagensPorMural} imagens." });

            byte[] bytes;
            using (var ms = new MemoryStream((int)arquivo.Length))
            {
                await arquivo.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            var tipo = TiposDeImagem.Identificar(bytes);
            if (tipo == null) return BadRequest(new { message = "Use uma imagem PNG, JPG ou WebP." });

            var imagem = new MuralImagem { MuralId = muralId, Tipo = tipo, Dados = bytes, CriadoEm = DateTime.UtcNow };
            await _uof.MuralRepository.AddImagemAsync(imagem);
            await _uof.CommitAsync();
            return Ok(new MuralImagemCriadaDto { ImagemId = imagem.MuralImagemId });
        }

        /// <summary>A imagem nunca muda depois de enviada (trocar = enviar outra), então pode ficar em cache à vontade.</summary>
        [HttpGet("{muralId:int}/imagens/{imagemId:int}")]
        public async Task<IActionResult> Imagem(int campanhaId, int muralId, int imagemId)
        {
            if (await SoMestreAsync(campanhaId) is { } erro) return erro;
            var etag = $"\"mural-{muralId}-{imagemId}\"";
            if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);

            var imagem = await _uof.MuralRepository.GetImagemAsync(muralId, imagemId);
            if (imagem == null) return NotFound("Imagem não encontrada.");

            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            return File(imagem.Dados, imagem.Tipo);
        }

        /// <summary>Todos os números em propriedades "imagemId" do documento, em qualquer profundidade.</summary>
        public static HashSet<int> ImagensUsadas(JsonElement raiz)
        {
            var ids = new HashSet<int>();
            void Visitar(JsonElement e)
            {
                switch (e.ValueKind)
                {
                    case JsonValueKind.Object:
                        foreach (var p in e.EnumerateObject())
                        {
                            if (p.NameEquals("imagemId") && p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var id)) ids.Add(id);
                            else Visitar(p.Value);
                        }
                        break;
                    case JsonValueKind.Array:
                        foreach (var item in e.EnumerateArray()) Visitar(item);
                        break;
                }
            }
            Visitar(raiz);
            return ids;
        }
    }
}
