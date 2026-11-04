using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories
{
    public interface IQtkdRepository
    {
        // Customer
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task AddCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);

        // Contract
        Task<Contract?> GetContractByIdAsync(int id);
        Task AddContractAsync(Contract contract);
        Task UpdateContractAsync(Contract contract);

        // Pricing
        Task<List<PriceList>> GetAllPriceListsAsync();
        Task<PriceList?> GetPriceListByIdAsync(int id);
        Task AddPriceListAsync(PriceList priceList);
        Task AddPriceItemAsync(PriceItem priceItem);
    }
}
