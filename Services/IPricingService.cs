using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services
{
    public interface IPricingService
    {
        Task<List<PriceList>> GetAllPriceListsAsync();
        Task<PriceList> GetPriceListByIdAsync(int id);
        Task<PriceList> CreatePriceListAsync(CreatePriceListDto dto);
        Task<PriceItem> AddPriceItemAsync(int priceListId, CreatePriceItemDto dto);
    }
}
