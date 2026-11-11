using System;
using System.Threading.Tasks;
using Moq;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;
using QTKD.Api.Services;

namespace QTKD.Tests.UnitTests
{
    public class ContractServiceRuleTests
    {
        private readonly Mock<IQtkdRepository> _mockRepo;
        private readonly ContractService _service;

        public ContractServiceRuleTests()
        {
            _mockRepo = new Mock<IQtkdRepository>();
            _service = new ContractService(_mockRepo.Object);
        }

        private static Contract CreateSampleContract(
            int id,
            ContractStatus status,
            string customerStatus = "Active",
            string? fileUrl = "https://storage.qtkd.vn/sample.pdf",
            string? content = "Cộng hòa xã hội chủ nghĩa Việt Nam - Nội dung hợp đồng chuẩn")
        {
            return new Contract
            {
                Id = id,
                ContractNo = $"HD-UNIT-{id}",
                CustomerId = 1,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = status,
                FileUrl = fileUrl,
                ContractContent = content,
                Customer = new Customer { Id = 1, Name = "Công ty Minh Phát", Status = customerStatus }
            };
        }

        // =========================================================================
        // 1. UNIT TESTS CHO QUY TẮC CTR-01 (Chỉ cho sửa ở Draft / RevisionRequested)
        // =========================================================================

        [Theory(DisplayName = "Unit - CTR01 (Đúng) - Cho phép sửa hợp đồng khi ở Draft hoặc RevisionRequested")]
        [InlineData(ContractStatus.Draft)]
        [InlineData(ContractStatus.RevisionRequested)]
        public async Task CTR01_UpdateAsync_WhenStatusAllowed_ShouldSucceed(ContractStatus allowedStatus)
        {
            // Arrange
            var contract = CreateSampleContract(101, allowedStatus);
            _mockRepo.Setup(r => r.GetContractByIdAsync(101)).ReturnsAsync(contract);

            var dto = new UpdateContractDto
            {
                ContractNo = "HD-UPDATED-101",
                FileUrl = "https://storage.qtkd.vn/updated.pdf"
            };

            // Act
            var result = await _service.UpdateAsync(101, dto);

            // Assert
            Assert.Equal("HD-UPDATED-101", result.ContractNo);
            Assert.Equal("https://storage.qtkd.vn/updated.pdf", result.FileUrl);
            _mockRepo.Verify(r => r.UpdateContractAsync(contract), Times.Once);
        }

        [Theory(DisplayName = "Unit - CTR01 (Sai) - Từ chối sửa khi hợp đồng ở PendingApproval hoặc Approved")]
        [InlineData(ContractStatus.PendingApproval)]
        [InlineData(ContractStatus.Approved)]
        [InlineData(ContractStatus.Rejected)]
        public async Task CTR01_UpdateAsync_WhenStatusDisallowed_ShouldThrowStateConflictException(ContractStatus blockedStatus)
        {
            // Arrange
            var contract = CreateSampleContract(102, blockedStatus);
            _mockRepo.Setup(r => r.GetContractByIdAsync(102)).ReturnsAsync(contract);

            var dto = new UpdateContractDto { ContractNo = "HD-HACK" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<StateConflictException>(() => _service.UpdateAsync(102, dto));
            Assert.Contains("CTR-01", ex.Message);
            _mockRepo.Verify(r => r.UpdateContractAsync(It.IsAny<Contract>()), Times.Never);
        }

        // =========================================================================
        // 2. UNIT TESTS CHO QUY TẮC CTR-02 (Đủ Customer Active, ngày hợp lệ, tệp & nội dung)
        // =========================================================================

        [Fact(DisplayName = "Unit - CTR02 (Đúng) - Submit thành công khi đáp ứng đủ toàn bộ điều kiện")]
        public async Task CTR02_SubmitContractAsync_WhenAllConditionsValid_ShouldSucceed()
        {
            // Arrange
            var contract = CreateSampleContract(201, ContractStatus.Draft);
            _mockRepo.Setup(r => r.GetContractByIdAsync(201)).ReturnsAsync(contract);

            // Act
            var result = await _service.SubmitContractAsync(201, "Sales", "KEY-201", null);

            // Assert
            Assert.Equal(201, result.ContractId);
            Assert.Equal("PendingApproval", result.Status);
            Assert.False(string.IsNullOrWhiteSpace(result.ApprovalId));
            _mockRepo.Verify(r => r.UpdateContractAsync(contract), Times.Once);
        }

        [Fact(DisplayName = "Unit - CTR02 (Sai) - Từ chối Submit khi thiếu FileUrl và thiếu Quốc hiệu")]
        public async Task CTR02_SubmitContractAsync_WhenMissingFileAndContent_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var contract = CreateSampleContract(202, ContractStatus.Draft, fileUrl: null, content: "Không có quốc hiệu");
            _mockRepo.Setup(r => r.GetContractByIdAsync(202)).ReturnsAsync(contract);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.SubmitContractAsync(202, "Sales", null, null));

            Assert.Equal(2, ex.Errors.Count);
            Assert.Contains(ex.Errors, e => e.Contains("FileUrl"));
            Assert.Contains(ex.Errors, e => e.Contains("Quốc hiệu"));
            Assert.Equal(ContractStatus.Draft, contract.Status);
            _mockRepo.Verify(r => r.UpdateContractAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact(DisplayName = "Unit - CTR02 (Sai) - Từ chối Submit khi Customer không Active và ngày hiệu lực sai")]
        public async Task CTR02_SubmitContractAsync_WhenCustomerInactiveAndInvalidDates_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var contract = CreateSampleContract(203, ContractStatus.Draft, customerStatus: "Inactive");
            contract.StartDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            contract.EndDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _mockRepo.Setup(r => r.GetContractByIdAsync(203)).ReturnsAsync(contract);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.SubmitContractAsync(203, "Sales", null, null));

            Assert.Equal(2, ex.Errors.Count);
            Assert.Contains(ex.Errors, e => e.Contains("Active"));
            Assert.Contains(ex.Errors, e => e.Contains("Ngày kết thúc phải sau ngày bắt đầu"));
            _mockRepo.Verify(r => r.UpdateContractAsync(It.IsAny<Contract>()), Times.Never);
        }

        // =========================================================================
        // 3. UNIT TESTS CHO QUY TẮC CTR-03 (Không chuyển Draft thẳng sang Approved)
        // =========================================================================

        [Fact(DisplayName = "Unit - CTR03 (Đúng) - Submit thành công chỉ chuyển từ Draft sang PendingApproval, không thành Approved")]
        public async Task CTR03_SubmitContractAsync_ShouldTransitionOnlyToPendingApproval()
        {
            // Arrange
            var contract = CreateSampleContract(301, ContractStatus.Draft);
            _mockRepo.Setup(r => r.GetContractByIdAsync(301)).ReturnsAsync(contract);

            // Act
            var response = await _service.SubmitContractAsync(301, "Sales", "KEY-301", null);

            // Assert
            Assert.Equal(ContractStatus.PendingApproval, contract.Status);
            Assert.NotEqual(ContractStatus.Approved, contract.Status);
            Assert.Equal("PendingApproval", response.Status);
        }

        [Fact(DisplayName = "Unit - CTR03 (Sai) - Từ chối chuyển trực tiếp từ Draft sang Approved qua UpdateAsync")]
        public async Task CTR03_UpdateAsync_WhenTryingToChangeDraftDirectlyToApproved_ShouldThrowStateConflictException()
        {
            // Arrange
            var contract = CreateSampleContract(302, ContractStatus.Draft);
            _mockRepo.Setup(r => r.GetContractByIdAsync(302)).ReturnsAsync(contract);

            var dto = new UpdateContractDto { Status = ContractStatus.Approved };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<StateConflictException>(() => _service.UpdateAsync(302, dto));
            Assert.Contains("CTR-03", ex.Message);
            Assert.Equal(ContractStatus.Draft, contract.Status);
            _mockRepo.Verify(r => r.UpdateContractAsync(It.IsAny<Contract>()), Times.Never);
        }

        // =========================================================================
        // 4. UNIT TESTS BỔ SUNG CHO AC01 & AC05 (Phân quyền & Gửi lặp Idempotency)
        // =========================================================================

        [Fact(DisplayName = "Unit - AC01 - Người dùng không có quyền Sales gửi trình duyệt ném ForbiddenException")]
        public async Task AC01_SubmitContractAsync_WithoutSalesRole_ShouldThrowForbiddenException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _service.SubmitContractAsync(401, "Guest", null, null));

            _mockRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact(DisplayName = "Unit - AC05 - Gửi lặp với cùng Idempotency-Key trả về ApprovalId cũ và không cập nhật lại DB")]
        public async Task AC05_SubmitContractAsync_WithSameIdempotencyKey_ShouldReturnExistingApprovalWithoutUpdatingDb()
        {
            // Arrange
            var contract = CreateSampleContract(402, ContractStatus.PendingApproval);
            contract.CurrentApprovalId = "APP-402-ORIGINAL";
            contract.LastIdempotencyKey = "KEY-SAME-402";
            _mockRepo.Setup(r => r.GetContractByIdAsync(402)).ReturnsAsync(contract);

            // Act
            var response = await _service.SubmitContractAsync(402, "Sales", "KEY-SAME-402", null);

            // Assert
            Assert.Equal("APP-402-ORIGINAL", response.ApprovalId);
            Assert.Equal("PendingApproval", response.Status);
            _mockRepo.Verify(r => r.UpdateContractAsync(It.IsAny<Contract>()), Times.Never);
        }
    }
}
