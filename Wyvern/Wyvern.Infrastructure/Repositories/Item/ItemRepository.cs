using Microsoft.EntityFrameworkCore;
using Wyvern.Infrastructure.Data;
using ItemEntity = Wyvern.Domain.Entities.Item;

namespace Wyvern.Infrastructure.Repositories.Item
{
    public class ItemRepository : IItemRepository
    {
        private readonly WyvernDbContext _context;

        public ItemRepository(WyvernDbContext context)
        {
            _context = context;
        }


        public async Task<ItemEntity> DeleteItemAsync(int id)
        {
            var item = await _context.Itens.FindAsync(id);

            if (item is null)
                throw new ArgumentNullException(nameof(item));

            item.Ativo = false;
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task<ItemEntity?> GetItemAsync(int id)
        {
            return await _context.Itens.FirstOrDefaultAsync(i => i.ItemId == id && i.Ativo);
        }

        public async Task<IEnumerable<ItemEntity>> GetItensAsync()
        {
            return await _context.Itens
                .Where(i => i.Ativo)
                .ToListAsync();
        }

        public async Task<ItemEntity> UpdateItemAsync(ItemEntity item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task<ItemEntity> CreateItemAsync(ItemEntity item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            _context.Itens.Add(item);
            await _context.SaveChangesAsync();

            return item;
        }

    }
}
