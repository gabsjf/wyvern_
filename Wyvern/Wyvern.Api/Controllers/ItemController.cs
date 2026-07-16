using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Wyvern.Application.DTOs.Item;
using Wyvern.Domain.Entities;
using Wyvern.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Wyvern.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly IUnitOfWork _uof;
        private readonly IMapper _mapper;
        public ItemController(IUnitOfWork uof, IMapper mapper)
        {
            _uof = uof;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetItens()
        {
            var itens = await _uof.ItemRepository.GetItensAsync();
            var itensDto = _mapper.Map<List<ItemResponseDto>>(itens);
            return Ok(itensDto);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemResponseDto>> GetItemById(int id)
        {
            var item = await _uof.ItemRepository.GetItemAsync(id);
            if( item == null)
            {
                return NotFound("Item nao encontrado");
            }
            var itemDto = _mapper.Map<ItemResponseDto>(item);
            return Ok(itemDto);
        }
        [HttpPost]
        public async Task<ActionResult<ItemResponseDto>> CreateItem(CreateItemDto itemDto)
        {
            if (itemDto == null)
            {
                return BadRequest("item inválido");
            }
            var item = _mapper.Map<Item>(itemDto);
            await _uof.ItemRepository.CreateItemAsync(item);
            var itemCriadoDto = _mapper.Map<ItemResponseDto>(item);
            return CreatedAtAction(nameof(GetItemById), new { id = item.ItemId }, itemCriadoDto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateItem(int id, ItemUpdateDto itemDto)
        {
            if (itemDto == null)
                return BadRequest("Dados inválidos");
            var itemNoBanco = await _uof.ItemRepository.GetItemAsync(id);
            if (itemNoBanco == null)
            {
                return BadRequest("Id do item não correspondente à rota");
            }
            _mapper.Map(itemDto, itemNoBanco);
            await _uof.ItemRepository.UpdateItemAsync(itemNoBanco);
            var itemAtualizado = await _uof.ItemRepository.GetItemAsync(id);
            var itemDtoAtualizado = _mapper.Map<ItemResponseDto>(itemAtualizado);
            return Ok(_mapper.Map<ItemResponseDto>(itemNoBanco));
            
        }
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteItem(int id)
        {
            var item = await _uof.ItemRepository.DeleteItemAsync(id);
            if( item == null)
            {
                return NotFound("item nao encontrado");

            }
            return Ok("item deletado com sucesso");
        }


    }
}
