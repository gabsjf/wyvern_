using System;
using System.Collections.Generic;
using System.Text;
using MagiaEntity = Wyvern.Domain.Entities.Magia;


namespace Wyvern.Domain.Interfaces.Repositories.Magia
{
    public interface IMagiaRepository
    {
        Task<IEnumerable<MagiaEntity>> GetMagiasAsync();
        Task<MagiaEntity?> GetMagiaByIdAsync(int id);
        Task<MagiaEntity> CreateMagiaAsync(MagiaEntity magia);
        Task<MagiaEntity> UpdateMagiaAsync(MagiaEntity magia);
        Task<MagiaEntity> DeleteMagiaAsync(int id);
    }
}
