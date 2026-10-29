using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services
{
    public class QtkdQueryService
    {
        private readonly IQtkdRepository _repository;

        public QtkdQueryService(IQtkdRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _repository.GetAllCustomersAsync();
        }

        public async Task<Contract> GetContractByIdAsync(int id)
        {
            var contract = await _repository.GetContractByIdAsync(id);
            
            if (contract == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng có ID = {id}");
            }
            
            return contract;
        }
    }
}