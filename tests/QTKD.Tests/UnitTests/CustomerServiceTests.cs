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
    public class CustomerServiceTests
    {
        private readonly Mock<IQtkdRepository> _mockRepo;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _mockRepo = new Mock<IQtkdRepository>();
            _service = new CustomerService(_mockRepo.Object);
        }

        [Fact(DisplayName = "Unit - Customer (Đúng) - Tạo Customer hợp lệ gọi AddCustomerAsync")]
        public async Task CreateAsync_WhenValid_ShouldTrimAndSaveCustomer()
        {
            var dto = new CreateCustomerDto { Name = "  Công ty Minh Phát  ", Status = "Active" };

            var result = await _service.CreateAsync(dto);

            Assert.Equal("Công ty Minh Phát", result.Name);
            Assert.Equal("Active", result.Status);
            _mockRepo.Verify(r => r.AddCustomerAsync(It.IsAny<Customer>()), Times.Once);
        }

        [Fact(DisplayName = "Unit - Customer (Sai) - Tạo Customer thiếu tên ném BusinessRuleException")]
        public async Task CreateAsync_WhenNameEmpty_ShouldThrowBusinessRuleException()
        {
            var dto = new CreateCustomerDto { Name = "   ", Status = "Active" };

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto));
            Assert.Contains("Tên khách hàng không được để trống", ex.Errors[0]);
            _mockRepo.Verify(r => r.AddCustomerAsync(It.IsAny<Customer>()), Times.Never);
        }

        [Fact(DisplayName = "Unit - Customer (Sai) - Cập nhật Customer không tồn tại ném KeyNotFoundException")]
        public async Task UpdateAsync_WhenCustomerNotFound_ShouldThrowKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetCustomerByIdAsync(999)).ReturnsAsync((Customer?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(999, new UpdateCustomerDto { Name = "ABC" }));
        }
    }
}
