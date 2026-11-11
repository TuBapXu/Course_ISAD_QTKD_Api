using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;
using QTKD.Api.Services;

namespace QTKD.Tests.UnitTests
{
    public class PricingServiceTests
    {
        private readonly Mock<IQtkdRepository> _mockRepo;
        private readonly PricingService _service;

        public PricingServiceTests()
        {
            _mockRepo = new Mock<IQtkdRepository>();
            _service = new PricingService(_mockRepo.Object);
        }

        [Fact(DisplayName = "Unit - Pricing (Đúng) - Thêm PriceItem hợp lệ với UnitPrice > 0")]
        public async Task AddPriceItemAsync_WhenValid_ShouldSucceed()
        {
            var priceList = new PriceList { Id = 1, Name = "Bảng giá 2026" };
            _mockRepo.Setup(r => r.GetPriceListByIdAsync(1)).ReturnsAsync(priceList);

            var result = await _service.AddPriceItemAsync(1, new CreatePriceItemDto
            {
                ServiceCode = "SV01",
                UnitPrice = 150000
            });

            Assert.Equal(1, result.PriceListId);
            Assert.Equal("SV01", result.ServiceCode);
            Assert.Equal(150000m, result.UnitPrice);
            _mockRepo.Verify(r => r.AddPriceItemAsync(It.IsAny<PriceItem>()), Times.Once);
        }

        [Theory(DisplayName = "Unit - Pricing (Sai) - Từ chối PriceItem khi UnitPrice <= 0")]
        [InlineData(0)]
        [InlineData(-50000)]
        public async Task AddPriceItemAsync_WhenUnitPriceInvalid_ShouldThrowBusinessRuleException(decimal badPrice)
        {
            var priceList = new PriceList { Id = 1, Name = "Bảng giá 2026" };
            _mockRepo.Setup(r => r.GetPriceListByIdAsync(1)).ReturnsAsync(priceList);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddPriceItemAsync(1, new CreatePriceItemDto
            {
                ServiceCode = "SV_BAD",
                UnitPrice = badPrice
            }));

            _mockRepo.Verify(r => r.AddPriceItemAsync(It.IsAny<PriceItem>()), Times.Never);
        }

        [Fact(DisplayName = "Unit - Pricing (Sai) - Thêm PriceItem vào PriceList không tồn tại ném KeyNotFoundException")]
        public async Task AddPriceItemAsync_WhenPriceListNotFound_ShouldThrowKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetPriceListByIdAsync(99)).ReturnsAsync((PriceList?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddPriceItemAsync(99, new CreatePriceItemDto
            {
                ServiceCode = "SV01",
                UnitPrice = 100000
            }));
        }
    }
}
