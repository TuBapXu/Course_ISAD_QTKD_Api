using QTKD.Api.Entities;

namespace QTKD.Api.Repositories
{
    public interface IQtkdRepository
    {
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Contract?> GetContractByIdAsync(int id);
    }
}