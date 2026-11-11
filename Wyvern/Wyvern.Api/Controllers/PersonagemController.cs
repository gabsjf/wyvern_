using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Paginacao;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Application.Mappings;
using Wyvern.Domain.Consultas;
using Wyvern.Domain.Entities;
using Wyvern.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Wyvern.Domain.Interfaces;
using Wyvern.Application.Services;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[Controller]")]
    public class PersonagemController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly ICampanhaAuthorizationService _campanhaAuth;
        private readonly ICronicaService _cronica;

        public PersonagemController (IUnitOfWork uof, IMapper mapper, ICurrentUserService currentUser, ICampanhaAuthorizationService campanhaAuth, ICronicaService cronica)
        {
            _uof = uof;
            _mapper = mapper;
            _currentUser = currentUser;
            _campanhaAuth = campanhaAuth;
            _cronica = cronica;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonagemResponseDto>>> GetPersonagens()
        {
            var personagens = await _uof.PersonagemRepository.GetPersonagensAsync();

            if (personagens == null || !personagens.Any())
            {
               
                return Ok(new List<PersonagemResponseDto>());
            }

            var personagensDto = _mapper.Map<List<PersonagemResponseDto>>(personagens);
            return Ok(personagensDto);
        }

        /// <summary>
        /// O elenco paginado, por campanha e nome. <paramref name="tipo"/> é "herois" (padrão) ou "monstros" (NPCs e monstros).
        /// Traz também as contagens das abas e de cada campanha, para a tela montar os filtros sem baixar tudo.
        /// </summary>
        [HttpGet("pagina")]
        public async Task<ActionResult<PaginaDePersonagensDto>> GetPagina(
            [FromQuery] int? pagina, [FromQuery] int? tamanho, [FromQuery] string? busca,
            [FromQuery] string? tipo, [FromQuery] int? campanhaId)
        {
            var herois = tipo is null or "" or "herois";
            if (!herois && tipo != "monstros")
                return BadRequest(new { message = "Tipo deve ser \"herois\" ou \"monstros\"." });

            var resultado = await _uof.ListagensRepository.PersonagensAsync(new PedidoDePagina(pagina, tamanho), busca, herois, campanhaId);
            return Ok(ListagensMapper.ParaDto(resultado, _mapper));
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PersonagemResponseDto>> GetPersonagemById( int id)
        {
            var personagens = await _uof.PersonagemRepository.GetPersonagemAsync(id);

            if (personagens == null )
            {

                return BadRequest("Personagem Não encontrado");
            }
            var personagemDto = _mapper.Map<PersonagemResponseDto>(personagens);
            return Ok(personagemDto);

        }

        [HttpPost]
        public async Task<ActionResult<PersonagemResponseDto>> CreatePersonagem(PersonagemCreateDto personagemDto)
        {
            if (personagemDto == null) return BadRequest("Dados inválidos");

            var personagem = _mapper.Map<Personagem>(personagemDto);
            personagem.CriadoEm = DateTime.Now;
            personagem.Ativo = true;
            EstadoPersonagemService.EncherEspacosDeMagiaNovos(personagem.PersonagemConjuracao);

            // GetCampanhaAsync já filtra por participação: nulo significa que a campanha não existe ou o usuário não faz parte dela.
            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(personagemDto.CampanhaId);
            if (campanha == null)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Você não participa desta campanha.");

            bool isMestre = await _campanhaAuth.IsMestreAsync(personagemDto.CampanhaId, _currentUser.UserId);

            if (!isMestre)
            {
                // Jogadores só podem criar tipo 1 (Jogador) e sem bloco NPC
                personagem.TipoId = 1;
                personagemDto.PersonagemNpc = null;
            }

            if (personagemDto.PericiasIds != null && personagemDto.PericiasIds.Any())
            {
                personagem.PersonagemPericias = personagemDto.PericiasIds.Select(id => new PersonagemPericia
                {
                    PericiaId = id
                }).ToList();
            }

            await _uof.PersonagemRepository.CreatePersonagemAsync(personagem);
            await _uof.CommitAsync();

            var retorno = await _uof.PersonagemRepository.GetPersonagemAsync(personagem.PersonagemId);

            if (retorno == null)
            {
                return CreatedAtAction(nameof(GetPersonagemById), new { id = personagem.PersonagemId }, null);
            }
            var retornoDto = _mapper.Map<PersonagemResponseDto>(retorno);
            return CreatedAtAction(nameof(GetPersonagemById), new { id = personagem.PersonagemId }, retornoDto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdatePersonagem(int id, PersonagemUpdateDto personagemDto)
        {
            var pBanco = await _uof.PersonagemRepository.GetPersonagemAsync(id);

            if (pBanco == null) return NotFound("Personagem não encontrado");

            // Só o Mestre muda o tipo do personagem (Jogador/NPC/Monstro) ou mexe no bloco de NPC.
            bool isMestre = await _campanhaAuth.IsMestreAsync(pBanco.CampanhaId, _currentUser.UserId);
            if (!isMestre)
            {
                personagemDto.TipoId = pBanco.TipoId;
                personagemDto.PersonagemNpc = null;
                // O que muda ao subir de nível só muda pela subida (ou pela correção do Mestre)
                ProgressaoService.ProtegerCamposDeNivel(pBanco, personagemDto);
            }

            pBanco.Nome = personagemDto.Nome;
            pBanco.Descricao = personagemDto.Descricao;
            pBanco.TipoId = personagemDto.TipoId;

            if (personagemDto.Atributo != null)
            {
                pBanco.Atributo ??= new Atributo { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.Atributo, pBanco.Atributo);
            }

            if (personagemDto.PersonagemPlayer != null)
            {
                pBanco.PersonagemPlayer ??= new PersonagemPlayer { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemPlayer, pBanco.PersonagemPlayer);
            }

            if (personagemDto.PersonagemCombate != null)
            {
                pBanco.PersonagemCombate ??= new PersonagemCombate { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemCombate, pBanco.PersonagemCombate);
            }

            if (personagemDto.PersonagemDetalhes != null)
            {
                pBanco.PersonagemDetalhes ??= new PersonagemDetalhes { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemDetalhes, pBanco.PersonagemDetalhes);
            }

            if (personagemDto.PersonagemDinheiro != null)
            {
                pBanco.PersonagemDinheiro ??= new PersonagemDinheiro { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemDinheiro, pBanco.PersonagemDinheiro);
            }

            if (personagemDto.PersonagemConjuracao != null)
            {
                pBanco.PersonagemConjuracao ??= new PersonagemConjuracao { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemConjuracao, pBanco.PersonagemConjuracao);
            }

            if (personagemDto.PersonagemNpc != null)
            {
                pBanco.PersonagemNpc ??= new PersonagemNpc { PersonagemId = pBanco.PersonagemId };
                _mapper.Map(personagemDto.PersonagemNpc, pBanco.PersonagemNpc);
            }

            if (personagemDto.PersonagemAcoesPadrao != null)
            {
                pBanco.PersonagemAcoesPadrao?.Clear();
                pBanco.PersonagemAcoesPadrao ??= new List<PersonagemAcaoPadrao>();
                foreach(var item in _mapper.Map<List<PersonagemAcaoPadrao>>(personagemDto.PersonagemAcoesPadrao))
                    pBanco.PersonagemAcoesPadrao.Add(item);
            }

            if (personagemDto.PersonagemAcoesBonus != null)
            {
                pBanco.PersonagemAcoesBonus?.Clear();
                pBanco.PersonagemAcoesBonus ??= new List<PersonagemAcaoBonus>();
                foreach(var item in _mapper.Map<List<PersonagemAcaoBonus>>(personagemDto.PersonagemAcoesBonus))
                    pBanco.PersonagemAcoesBonus.Add(item);
            }

            if (personagemDto.PersonagemReacoes != null)
            {
                pBanco.PersonagemReacoes?.Clear();
                pBanco.PersonagemReacoes ??= new List<PersonagemReacao>();
                foreach(var item in _mapper.Map<List<PersonagemReacao>>(personagemDto.PersonagemReacoes))
                    pBanco.PersonagemReacoes.Add(item);
            }

            if (personagemDto.PersonagemAcoesLendarias != null)
            {
                pBanco.PersonagemAcoesLendarias?.Clear();
                pBanco.PersonagemAcoesLendarias ??= new List<PersonagemAcaoLendaria>();
                foreach(var item in _mapper.Map<List<PersonagemAcaoLendaria>>(personagemDto.PersonagemAcoesLendarias))
                    pBanco.PersonagemAcoesLendarias.Add(item);
            }

            if (personagemDto.PersonagemTracosEspeciais != null)
            {
                pBanco.PersonagemTracosEspeciais?.Clear();
                pBanco.PersonagemTracosEspeciais ??= new List<PersonagemTracoEspecial>();
                foreach(var item in _mapper.Map<List<PersonagemTracoEspecial>>(personagemDto.PersonagemTracosEspeciais))
                    pBanco.PersonagemTracosEspeciais.Add(item);
            }

            if (personagemDto.PersonagemAtaques != null)
            {
                pBanco.PersonagemAtaques?.Clear();
                pBanco.PersonagemAtaques ??= new List<PersonagemAtaque>();
                foreach(var item in _mapper.Map<List<PersonagemAtaque>>(personagemDto.PersonagemAtaques))
                    pBanco.PersonagemAtaques.Add(item);
            }

            if (personagemDto.PersonagemMagias != null)
            {
                pBanco.PersonagemMagias?.Clear();
                pBanco.PersonagemMagias ??= new List<PersonagemMagia>();
                foreach(var item in _mapper.Map<List<PersonagemMagia>>(personagemDto.PersonagemMagias))
                    pBanco.PersonagemMagias.Add(item);
            }

            if (personagemDto.PericiasIds != null)
            {
                pBanco.PersonagemPericias?.Clear();
                pBanco.PersonagemPericias ??= new List<PersonagemPericia>();
                foreach(var pId in personagemDto.PericiasIds)
                    pBanco.PersonagemPericias.Add(new PersonagemPericia { PericiaId = pId, PersonagemId = pBanco.PersonagemId });
            }

            await _uof.PersonagemRepository.UpdatePersonagemAsync(pBanco);
            await _uof.CommitAsync();
            return Ok(_mapper.Map<PersonagemResponseDto>(pBanco));
        }

        /// <summary>O que muda no próximo nível (regras do D&D 2024) e se o personagem já pode subir. Dono ou Mestre.</summary>
        [HttpGet("{id:int}/proximo-nivel")]
        public async Task<ActionResult<ProximoNivelDto>> ProximoNivel(int id)
        {
            var personagem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");
            return Ok(ProgressaoService.Previa(personagem));
        }

        /// <summary>
        /// Sobe um nível com as escolhas do jogador (PV fixo ou rolado, subclasse, melhoria de atributo ou talento).
        /// Só quando o Mestre já liberou o nível (por XP ou por marco). Dono ou Mestre.
        /// </summary>
        [HttpPost("{id:int}/subir-nivel")]
        public async Task<ActionResult<ResultadoSubidaDto>> SubirNivel(int id, SubirNivelDto dto)
        {
            var personagem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");

            var (pvGanhos, resumo) = ProgressaoService.Aplicar(personagem, dto);
            var nivelNovo = personagem.PersonagemPlayer!.Nivel;
            await _cronica.RegistrarAutomaticoAsync(personagem.CampanhaId, "Marco", $"{personagem.Nome} subiu para o nível {nivelNovo}", resumo);

            await _uof.PersonagemRepository.UpdatePersonagemAsync(personagem);
            await _uof.CommitAsync();
            return Ok(new ResultadoSubidaDto
            {
                Personagem = _mapper.Map<PersonagemResponseDto>(personagem),
                NivelNovo = nivelNovo,
                PvGanhos = pvGanhos,
                Resumo = resumo,
            });
        }

        /// <summary>Só o Mestre: ajusta o XP e/ou libera um nível por marco.</summary>
        [HttpPut("{id:int}/progressao")]
        public async Task<ActionResult<PersonagemResponseDto>> AtualizarProgressao(int id, ProgressaoDto dto)
        {
            var personagem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");
            if (!await _campanhaAuth.IsMestreAsync(personagem.CampanhaId, _currentUser.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só o Mestre da campanha libera níveis e dá XP.");
            if (personagem.TipoId != 1 || personagem.PersonagemPlayer == null)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Só personagens de jogador têm XP e nível.");

            var player = personagem.PersonagemPlayer;
            if (dto.Xp is { } xp)
            {
                if (xp < 0 || xp > 10_000_000) return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "XP inválido.");
                player.Xp = xp;
            }
            if (dto.NivelLiberado is { } liberado)
            {
                if (liberado < 0 || liberado > RegrasDnd2024.NivelMaximo)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "O nível liberado vai de 0 a 20.");
                player.NivelLiberado = liberado;
            }

            await _uof.PersonagemRepository.UpdatePersonagemAsync(personagem);
            await _uof.CommitAsync();
            return Ok(_mapper.Map<PersonagemResponseDto>(personagem));
        }

        /// <summary>
        /// Registra um descanso curto ou longo. O repositório só devolve o personagem ao dono ou ao Mestre da campanha
        /// (para os demais ele não existe), então essa é a mesma regra de quem pode editar a ficha.
        /// </summary>
        [HttpPost("{id:int}/descanso")]
        public async Task<ActionResult<PersonagemResponseDto>> Descansar(int id, PersonagemDescansoDto dto)
        {
            var personagem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");

            var vidaAntes = personagem.PersonagemCombate?.VidaAtual ?? 0;
            DescansoService.Aplicar(personagem, dto);

            // O descanso também fica na crônica da campanha
            var longo = string.Equals(dto.Tipo, "Longo", StringComparison.OrdinalIgnoreCase);
            var vidaDepois = personagem.PersonagemCombate!.VidaAtual;
            var detalhe = vidaDepois != vidaAntes ? $"PV {vidaAntes} → {vidaDepois}." : "Sem mudança nos PV.";
            await _cronica.RegistrarAutomaticoAsync(personagem.CampanhaId, "Descanso",
                $"{personagem.Nome} fez um descanso {(longo ? "longo" : "curto")}", detalhe);

            await _uof.PersonagemRepository.UpdatePersonagemAsync(personagem);
            await _uof.CommitAsync();
            return Ok(_mapper.Map<PersonagemResponseDto>(personagem));
        }

        /// <summary>
        /// Copia a ficha inteira para um personagem novo, na mesma campanha ou em outra (biblioteca de criaturas).
        /// Quem copia precisa ser Mestre da campanha de destino e ter acesso ao original.
        /// </summary>
        [HttpPost("{id:int}/duplicar")]
        public async Task<ActionResult<PersonagemResponseDto>> Duplicar(int id, PersonagemDuplicarDto? dto)
        {
            var origem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (origem == null) return NotFound("Personagem não encontrado");

            var destino = dto?.CampanhaId ?? origem.CampanhaId;
            if (!await _campanhaAuth.IsMestreAsync(destino, _currentUser.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Só o Mestre da campanha de destino pode adicionar personagens a ela.");

            var nome = string.IsNullOrWhiteSpace(dto?.Nome)
                ? (destino == origem.CampanhaId ? $"{origem.Nome} (cópia)" : origem.Nome)
                : dto!.Nome!.Trim();

            var copia = PersonagemClonador.Clonar(origem, destino, nome, _currentUser.UserId ?? origem.CriadoPorId);
            await _uof.PersonagemRepository.CreatePersonagemAsync(copia);
            await _uof.CommitAsync();

            var salvo = await _uof.PersonagemRepository.GetPersonagemAsync(copia.PersonagemId);
            return Ok(_mapper.Map<PersonagemResponseDto>(salvo));
        }

        /// <summary>
        /// O jogador traz um herói que já tem (de outra campanha) para uma campanha de que participa, por exemplo ao entrar
        /// por convite. Vai uma cópia: a ficha original continua na campanha antiga. A cópia chega descansada e com o mesmo
        /// nível; a crônica da campanha nova registra a chegada para o Mestre ver e ajustar, se quiser.
        /// </summary>
        [HttpPost("{id:int}/trazer")]
        public async Task<ActionResult<PersonagemResponseDto>> Trazer(int id, PersonagemTrazerDto dto)
        {
            // GetPersonagemAsync só devolve o que o usuário pode ver; aqui vale só o que é dele e é herói
            var origem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (origem == null || origem.CriadoPorId != _currentUser.UserId)
                return NotFound("Personagem não encontrado");
            if (origem.TipoId != 1)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Só heróis podem ser levados para outra campanha.");
            if (origem.CampanhaId == dto.CampanhaId)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Este personagem já está nesta campanha.");
            if (!await _campanhaAuth.IsMembroAsync(dto.CampanhaId, _currentUser.UserId))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Você só pode trazer personagens para campanhas de que participa.");

            var copia = PersonagemClonador.Clonar(origem, dto.CampanhaId, origem.Nome, origem.CriadoPorId);
            PersonagemClonador.PrepararParaNovaCampanha(copia);
            await _uof.PersonagemRepository.CreatePersonagemAsync(copia);

            var player = copia.PersonagemPlayer;
            var quem = player == null ? "Um herói" : $"{player.Classe} de nível {player.Nivel}".Trim();
            await _cronica.RegistrarAutomaticoAsync(dto.CampanhaId, "Outro", $"{copia.Nome} chegou à campanha",
                $"{quem} trazido de outra campanha pelo jogador. O Mestre pode ajustar a ficha se o nível não combinar com a mesa.");
            await _uof.CommitAsync();

            var salvo = await _uof.PersonagemRepository.GetPersonagemAsync(copia.PersonagemId);
            return Ok(_mapper.Map<PersonagemResponseDto>(salvo));
        }

        /// <summary>
        /// Muda só o estado de jogo da ficha (PV, condições, inspiração, espaços de magia...), sem reenviar a ficha inteira.
        /// Mesma regra de acesso do resto: dono ou Mestre da campanha.
        /// </summary>
        [HttpPut("{id:int}/estado")]
        public async Task<ActionResult<PersonagemResponseDto>> AtualizarEstado(int id, PersonagemEstadoDto dto)
        {
            var personagem = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");

            EstadoPersonagemService.Aplicar(personagem, dto);

            await _uof.PersonagemRepository.UpdatePersonagemAsync(personagem);
            await _uof.CommitAsync();
            return Ok(_mapper.Map<PersonagemResponseDto>(personagem));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeletePersonagem(int id)
        {
            // GetPersonagemAsync só devolve personagens do próprio usuário ou de campanhas em que ele é Mestre.
            var permitido = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (permitido == null) return NotFound("Personagem não encontrado");

            await _uof.PersonagemRepository.DeletePersonagemAsync(id);
            await _uof.CommitAsync();
            return Ok(new { mensagem = "Personagem desativado com sucesso", id });
        }

        private const long TamanhoMaximoPdf = 5 * 1024 * 1024;

        [HttpPost("import-pdf")]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(TamanhoMaximoPdf + 64 * 1024)]
        public async Task<ActionResult<PersonagemResponseDto>> ImportPdf(IFormFile file, [FromForm] int campanhaId, [FromServices] Wyvern.Application.Services.IPdfParserService pdfParserService)
        {
            if (file == null || file.Length == 0) return BadRequest("Nenhum arquivo enviado.");
            if (file.Length > TamanhoMaximoPdf) return BadRequest("O PDF deve ter no máximo 5 MB.");

            // Confere a assinatura "%PDF-" em vez de confiar na extensão ou no Content-Type.
            var cabecalho = new byte[5];
            using (var leitura = file.OpenReadStream())
            {
                if (await leitura.ReadAsync(cabecalho, 0, cabecalho.Length) < cabecalho.Length
                    || System.Text.Encoding.ASCII.GetString(cabecalho) != "%PDF-")
                    return BadRequest("O arquivo enviado não é um PDF válido.");
            }

            var campanha = await _uof.CampanhaRepository.GetCampanhaAsync(campanhaId);
            if (campanha == null) return BadRequest("Campanha inválida ou você não tem acesso a ela.");

            if (!_currentUser.UserId.HasValue) return Unauthorized();

            try
            {
                using var stream = file.OpenReadStream();
                var personagem = pdfParserService.ParsePdf(stream, campanhaId, _currentUser.UserId.Value);

                // O PDF traz perícias só pelo nome: liga ao cadastro (aceita "Lidar com" / "Adestrar Animais") e ignora o que não existe.
                if (personagem.PersonagemPericias?.Count > 0)
                {
                    var catalogo = (await _uof.PericiaRepository.GetPericiasAsync()).ToList();
                    var ligadas = new List<PersonagemPericia>();
                    foreach (var pp in personagem.PersonagemPericias)
                    {
                        var achada = catalogo.FirstOrDefault(c => Wyvern.Application.Services.FichaPdfMapper.MesmaPericia(c.Nome, pp.Pericia?.Nome));
                        if (achada == null || ligadas.Any(l => l.PericiaId == achada.PericiaId)) continue;
                        ligadas.Add(new PersonagemPericia { PericiaId = achada.PericiaId, Bonus = pp.Bonus, Proficiente = true });
                    }
                    personagem.PersonagemPericias = ligadas;
                }

                if (!await _campanhaAuth.IsMestreAsync(campanhaId, _currentUser.UserId))
                {
                    personagem.TipoId = 1;
                    personagem.PersonagemNpc = null;
                }

                // Salvar no banco
                await _uof.PersonagemRepository.CreatePersonagemAsync(personagem);
                await _uof.CommitAsync();

                var retorno = await _uof.PersonagemRepository.GetPersonagemAsync(personagem.PersonagemId);
                var retornoDto = _mapper.Map<PersonagemResponseDto>(retorno);
                
                return Ok(retornoDto);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao importar PDF: {ex.Message}");
            }
        }

        [HttpGet("{id:int}/export-pdf")]
        public async Task<IActionResult> ExportPdf(int id, [FromServices] Wyvern.Application.Services.IPdfExportService pdfExportService)
        {
            var pBanco = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (pBanco == null) return NotFound("Personagem não encontrado");

            try
            {
                var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "ficha55.pdf");
                var pdfBytes = pdfExportService.ExportPdf(pBanco, templatePath);
                return File(pdfBytes, "application/pdf", $"{pBanco.Nome ?? "Personagem"}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao exportar PDF: {ex.Message}");
            }
        }

        [HttpPost("{id:int}/items")]
        public async Task<ActionResult> AddItem(int id, PersonagemItemAddDto dto)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();
            
            p.PersonagemItens ??= new List<PersonagemItem>();
            var item = _mapper.Map<PersonagemItem>(dto);
            item.PersonagemId = id;
            p.PersonagemItens.Add(item);
            await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
            await _uof.CommitAsync();

            return Ok();
        }

        [HttpDelete("{id:int}/items/{itemId:int}")]
        public async Task<ActionResult> RemoveItem(int id, int itemId)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();

            var item = p.PersonagemItens?.FirstOrDefault(i => i.PersonagemItemId == itemId);
            if (item != null)
            {
                p.PersonagemItens!.Remove(item);
                await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
                await _uof.CommitAsync();
            }
            return Ok();
        }

        [HttpPost("{id:int}/magias")]
        public async Task<ActionResult> AddMagia(int id, PersonagemMagiaAddDto dto)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();
            
            p.PersonagemMagias ??= new List<PersonagemMagia>();
            var magia = _mapper.Map<PersonagemMagia>(dto);
            magia.PersonagemId = id;
            p.PersonagemMagias.Add(magia);
            await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
            await _uof.CommitAsync();

            return Ok();
        }

        [HttpDelete("{id:int}/magias/{magiaId:int}")]
        public async Task<ActionResult> RemoveMagia(int id, int magiaId)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();

            var magia = p.PersonagemMagias?.FirstOrDefault(m => m.PersonagemMagiaId == magiaId);
            if (magia != null)
            {
                p.PersonagemMagias!.Remove(magia);
                await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
                await _uof.CommitAsync();
            }
            return Ok();
        }

        [HttpPost("{id:int}/ataques")]
        public async Task<ActionResult> AddAtaque(int id, PersonagemAtaqueCreateDto dto)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();
            
            p.PersonagemAtaques ??= new List<PersonagemAtaque>();
            var ataque = _mapper.Map<PersonagemAtaque>(dto);
            ataque.PersonagemId = id;
            p.PersonagemAtaques.Add(ataque);
            await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
            await _uof.CommitAsync();

            return Ok();
        }

        [HttpDelete("{id:int}/ataques/{ataqueId:int}")]
        public async Task<ActionResult> RemoveAtaque(int id, int ataqueId)
        {
            var p = await _uof.PersonagemRepository.GetPersonagemAsync(id);
            if (p == null) return NotFound();

            var ataque = p.PersonagemAtaques?.FirstOrDefault(a => a.PersonagemAtaqueId == ataqueId);
            if (ataque != null)
            {
                p.PersonagemAtaques!.Remove(ataque);
                await _uof.PersonagemRepository.UpdatePersonagemAsync(p);
                await _uof.CommitAsync();
            }
            return Ok();
        }

    }
}
