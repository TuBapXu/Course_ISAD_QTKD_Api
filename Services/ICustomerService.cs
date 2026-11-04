using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer> GetByIdAsync(int id);
        Task<Customer> CreateAsync(CreateCustomerDto dto);
        Task<Customer> UpdateAsync(int id, UpdateCustomerDto dto);
    }
}
