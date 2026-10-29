using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers
{
    // Đường dẫn gốc: api/customers
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly QtkdQueryService _queryService;
        public CustomersController(QtkdQueryService queryService)
        {
            _queryService = queryService;
        }

        // Endpoint GET /api/customers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Gọi hàm từ Service và trả về mã 200 OK kèm danh sách
            var customers = await _queryService.GetAllCustomersAsync();
            return Ok(customers);
        }
    }
}