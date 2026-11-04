using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractsController(IContractService contractService)
        {
            _contractService = contractService;
        }

        // GET /api/contracts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            return Ok(contract);
        }

        // POST /api/contracts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContractDto dto)
        {
            var created = await _contractService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/contracts/{id} - Kiểm tra CTR-01
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContractDto dto)
        {
            var updated = await _contractService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        // POST /api/contracts/{id}/submit - Hiện thực US03 / CTR-02 / CTR-03
        [HttpPost("{id}/submit")]
        public async Task<IActionResult> Submit(
            int id,
            [FromHeader(Name = "X-User-Role")] string? userRole,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            [FromBody] SubmitContractRequestDto? dto)
        {
            // Nếu người dùng không truyền header X-User-Role, ta cũng có thể kiểm tra xem có Bearer token không;
            // nhưng để thuận tiện kiểm thử Swagger, ta ưu tiên đọc X-User-Role.
            var result = await _contractService.SubmitContractAsync(id, userRole, idempotencyKey, dto);
            return Ok(result);
        }
    }
}
