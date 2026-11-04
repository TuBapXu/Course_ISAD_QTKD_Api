using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services
{
    public interface IContractService
    {
        Task<Contract> GetByIdAsync(int id);
        Task<Contract> CreateAsync(CreateContractDto dto);
        Task<Contract> UpdateAsync(int id, UpdateContractDto dto);
        Task<SubmitContractResponseDto> SubmitContractAsync(int id, string? userRole, string? idempotencyKey, SubmitContractRequestDto? dto);
    }
}
