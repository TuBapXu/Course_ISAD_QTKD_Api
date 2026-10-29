using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly QtkdQueryService _queryService;

        public ContractsController(QtkdQueryService queryService)
        {
            _queryService = queryService;
        }

        // Endpoint GET /api/contracts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Gọi Service lấy chi tiết hợp đồng
            // Nếu lỗi KeyNotFoundException xảy ra trong Service, Middleware sẽ tự biến nó thành lỗi 404
            var contract = await _queryService.GetContractByIdAsync(id);
            return Ok(contract);
        }
    }
}