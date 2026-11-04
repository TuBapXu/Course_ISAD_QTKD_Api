using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PricesController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricesController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPriceLists()
        {
            var priceLists = await _pricingService.GetAllPriceListsAsync();
            return Ok(priceLists);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPriceListById(int id)
        {
            var priceList = await _pricingService.GetPriceListByIdAsync(id);
            return Ok(priceList);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListDto dto)
        {
            var created = await _pricingService.CreatePriceListAsync(dto);
            return CreatedAtAction(nameof(GetPriceListById), new { id = created.Id }, created);
        }

        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddPriceItem(int id, [FromBody] CreatePriceItemDto dto)
        {
            var createdItem = await _pricingService.AddPriceItemAsync(id, dto);
            return Ok(createdItem);
        }
    }
}
