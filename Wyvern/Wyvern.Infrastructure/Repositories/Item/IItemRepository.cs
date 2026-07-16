using System;
using System.Collections.Generic;
using System.Text;
using ItemEntity = Wyvern.Domain.Entities.Item;


namespace Wyvern.Infrastructure.Repositories.Item
{
    public interface IItemRepository
    {
        Task<IEnumerable<ItemEntity>> GetItensAsync();
        Task<ItemEntity?> GetItemAsync(int id);
        Task<ItemEntity> CreateItemAsync(ItemEntity item);
        Task<ItemEntity> UpdateItemAsync(ItemEntity item);
        Task<ItemEntity> DeleteItemAsync(int id);
    }
}
