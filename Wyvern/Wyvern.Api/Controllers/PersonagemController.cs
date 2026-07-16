using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Personagem;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[Controller]")]
    public class PersonagemController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        public PersonagemController (IUnitOfWork uof, IMapper mapper)
        {
            _uof = uof;
            _mapper = mapper;
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

            if (personagemDto.PericiasIds != null && personagemDto.PericiasIds.Any())
            {
                personagem.PersonagemPericias = personagemDto.PericiasIds.Select(id => new PersonagemPericia
                {
                    PericiaId = id
                }).ToList();
            }

            await _uof.PersonagemRepository.CreatePersonagemAsync(personagem);

            
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
            return Ok(_mapper.Map<PersonagemResponseDto>(pBanco));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeletePersonagem(int id)
        {
            var personagem = await _uof.PersonagemRepository.DeletePersonagemAsync(id);
            if (personagem == null) return NotFound("Personagem não encontrado");
            return Ok(new { mensagem = "Personagem desativado com sucesso", id });
        }

        [HttpPost("import-pdf")]
        public async Task<ActionResult<PersonagemResponseDto>> ImportPdf(IFormFile file, [FromServices] Wyvern.Application.Services.IPdfParserService pdfParserService)
        {
            if (file == null || file.Length == 0) return BadRequest("Nenhum arquivo enviado.");

            try
            {
                using var stream = file.OpenReadStream();
                var personagem = pdfParserService.ParsePdf(stream);
                
                // Salvar no banco
                await _uof.PersonagemRepository.CreatePersonagemAsync(personagem);
                
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
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ficha55.pdf");
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
            }
            return Ok();
        }

    }
}
