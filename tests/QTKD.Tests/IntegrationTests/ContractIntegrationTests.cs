using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Tests.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class ContractIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public ContractIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // Helper tạo nhanh 1 Contract mới ở trạng thái Draft cho từng test case độc lập
        private async Task<Contract> CreateDraftContractAsync(string? fileUrl = null, string? content = null)
        {
            var dto = new CreateContractDto
            {
                ContractNo = $"HD-TEST-{Guid.NewGuid():N}"[..16],
                CustomerId = 1, // Customer mẫu Id = 1 (Active) từ migration seed
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FileUrl = fileUrl,
                ContractContent = content
            };

            var res = await _client.PostAsJsonAsync("/api/contracts", dto, JsonOptions);
            res.EnsureSuccessStatusCode();
            var contract = await res.Content.ReadFromJsonAsync<Contract>(JsonOptions);
            Assert.NotNull(contract);
            return contract!;
        }

        private HttpRequestMessage BuildSubmitRequest(int contractId, string? role, string? idempotencyKey = null, string? comment = null)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/contracts/{contractId}/submit");
            if (role != null)
                req.Headers.Add("X-User-Role", role);
            if (idempotencyKey != null)
                req.Headers.Add("Idempotency-Key", idempotencyKey);

            req.Content = JsonContent.Create(new SubmitContractRequestDto { Comment = comment ?? "Trình duyệt kiểm thử" });
            return req;
        }

        [Fact(DisplayName = "TC03 - Sửa Contract ở Draft thành công khi dữ liệu hợp lệ")]
        public async Task TC03_UpdateContract_WhenDraftAndValid_ShouldReturn200_AndPersistInDb()
        {
            // Arrange
            var contract = await CreateDraftContractAsync();
            var updateDto = new UpdateContractDto
            {
                ContractNo = $"{contract.ContractNo}-UPD",
                FileUrl = "https://storage.qtkd.vn/test-tc03.pdf",
                ContractContent = "Cộng hòa xã hội chủ nghĩa Việt Nam - Hợp đồng sửa thành công"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", updateDto, JsonOptions);

            // Assert HTTP Status + Body
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<Contract>(JsonOptions);
            Assert.NotNull(updated);
            Assert.Equal(updateDto.ContractNo, updated!.ContractNo);
            Assert.Equal(updateDto.FileUrl, updated.FileUrl);
            Assert.Equal(ContractStatus.Draft, updated.Status);

            // Assert DB
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(updateDto.ContractNo, inDb!.ContractNo);
            Assert.Equal(updateDto.FileUrl, inDb.FileUrl);
            Assert.Equal(updateDto.ContractContent, inDb.ContractContent);
        }

        [Fact(DisplayName = "TC04 - Sửa Contract ở PendingApproval bị từ chối 409 và dữ liệu giữ nguyên (CTR-01)")]
        public async Task TC04_UpdateContract_WhenPendingApproval_ShouldReturn409_AndKeepOriginalData()
        {
            // Arrange: Tạo contract đủ điều kiện và submit sang PendingApproval
            var originalFileUrl = "https://storage.qtkd.vn/tc04-original.pdf";
            var originalContent = "Cộng hòa xã hội chủ nghĩa Việt Nam - Nội dung gốc";
            var contract = await CreateDraftContractAsync(originalFileUrl, originalContent);

            var submitRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", $"KEY-TC04-{contract.Id}"));
            Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);

            // Act: Cố tình sửa Contract khi đang PendingApproval
            var updateDto = new UpdateContractDto
            {
                ContractNo = "HD-BI-SUA-TRAI-PHEP",
                FileUrl = "https://storage.qtkd.vn/hack.pdf"
            };
            var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", updateDto, JsonOptions);

            // Assert HTTP Status + Error Body
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(errorBody);
            Assert.Contains("CTR-01", errorBody!.Error);

            // Assert DB: Dữ liệu giữ nguyên không bị ghi đè
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.PendingApproval, inDb!.Status);
            Assert.Equal(contract.ContractNo, inDb.ContractNo);
            Assert.Equal(originalFileUrl, inDb.FileUrl);
        }

        [Fact(DisplayName = "TC05 - Submit Contract thiếu điều kiện CTR-02 bị từ chối 400 và vẫn là Draft")]
        public async Task TC05_SubmitContract_WhenMissingCtr02Conditions_ShouldReturn400WithErrors_AndRemainDraft()
        {
            // Arrange: Contract Draft thiếu FileUrl và thiếu Quốc hiệu
            var contract = await CreateDraftContractAsync(fileUrl: null, content: "Nội dung thiếu Quốc hiệu");

            // Act
            var response = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales"));

            // Assert HTTP Status + Body (trả danh sách điều kiện chưa đạt)
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(errorBody);
            Assert.NotNull(errorBody!.Errors);
            Assert.Equal(2, errorBody.Errors!.Count);
            Assert.Contains(errorBody.Errors, e => e.Contains("FileUrl"));
            Assert.Contains(errorBody.Errors, e => e.Contains("Quốc hiệu"));

            // Assert DB: Vẫn là Draft và chưa có ApprovalId
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.Draft, inDb!.Status);
            Assert.Null(inDb.CurrentApprovalId);
        }

        [Fact(DisplayName = "TC06 - Submit Contract hợp lệ chuyển sang PendingApproval và tạo ApprovalId (CTR-03 & AC04)")]
        public async Task TC06_SubmitContract_WhenValid_ShouldReturn200_AndUpdateDbToPendingApproval()
        {
            // Arrange
            var contract = await CreateDraftContractAsync(
                fileUrl: "https://storage.qtkd.vn/tc06-valid.pdf",
                content: "Cộng hòa xã hội chủ nghĩa Việt Nam - Hợp đồng hợp lệ TC06");

            // Act
            var response = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", $"KEY-TC06-{contract.Id}"));

            // Assert HTTP Status + JSON Body
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SubmitContractResponseDto>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(contract.Id, result!.ContractId);
            Assert.Equal("PendingApproval", result.Status);
            Assert.False(string.IsNullOrWhiteSpace(result.ApprovalId));

            // Assert DB: Trạng thái thực tế trong PostgreSQL test DB là PendingApproval
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.PendingApproval, inDb!.Status);
            Assert.Equal(result.ApprovalId, inDb.CurrentApprovalId);
        }

        [Fact(DisplayName = "TC07 - Thử chuyển Draft thẳng sang Approved bị từ chối 409 (CTR-03)")]
        public async Task TC07_TransitionDraftDirectlyToApproved_ShouldBeRejected_AndRemainDraft()
        {
            // Arrange: Hợp đồng ở Draft dù đã đủ nội dung
            var contract = await CreateDraftContractAsync(
                fileUrl: "https://storage.qtkd.vn/tc07.pdf",
                content: "Cộng hòa xã hội chủ nghĩa Việt Nam - Test CTR-03");

            var illegalUpdate = new UpdateContractDto
            {
                Status = ContractStatus.Approved
            };

            // Act: Cố tình gửi yêu cầu đổi trạng thái trực tiếp từ Draft -> Approved
            var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", illegalUpdate, JsonOptions);

            // Assert HTTP Status + Error Body
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(errorBody);
            Assert.Contains("CTR-03", errorBody!.Error);

            // Assert DB: Trạng thái vẫn là Draft, không nhảy sang Approved
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.Draft, inDb!.Status);
        }

        [Fact(DisplayName = "TC08 - Submit lặp cùng Idempotency-Key hoặc sai trạng thái không tạo trùng phiên duyệt (AC05)")]
        public async Task TC08_SubmitContractTwice_ShouldNotCreateDuplicateApproval()
        {
            // Arrange
            var contract = await CreateDraftContractAsync(
                fileUrl: "https://storage.qtkd.vn/tc08.pdf",
                content: "Cộng hòa xã hội chủ nghĩa Việt Nam - Test Idempotency TC08");
            var idempotencyKey = $"KEY-TC08-{Guid.NewGuid():N}";

            // Act 1: Submit lần đầu
            var firstRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", idempotencyKey));
            Assert.Equal(HttpStatusCode.OK, firstRes.StatusCode);
            var firstBody = await firstRes.Content.ReadFromJsonAsync<SubmitContractResponseDto>(JsonOptions);
            Assert.NotNull(firstBody);

            // Act 2: Submit lặp lại với CÙNG Idempotency-Key -> Phải trả về cùng ApprovalId
            var secondRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", idempotencyKey));
            Assert.Equal(HttpStatusCode.OK, secondRes.StatusCode);
            var secondBody = await secondRes.Content.ReadFromJsonAsync<SubmitContractResponseDto>(JsonOptions);
            Assert.NotNull(secondBody);
            Assert.Equal(firstBody!.ApprovalId, secondBody!.ApprovalId);

            // Act 3: Submit lặp lại KHÔNG có Idempotency-Key -> Phải từ chối 409 Conflict (AC02)
            var thirdRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", idempotencyKey: null));
            Assert.Equal(HttpStatusCode.Conflict, thirdRes.StatusCode);

            // Assert DB: ApprovalId lưu trong DB vẫn là phiên duy nhất từ lần 1
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.PendingApproval, inDb!.Status);
            Assert.Equal(firstBody.ApprovalId, inDb.CurrentApprovalId);
        }

        [Fact(DisplayName = "TC08b (Slide 20 TC06) - Hai yêu cầu Submit gửi đồng thời không tạo hai workflow")]
        public async Task TC08b_ConcurrentSubmit_ShouldOnlyCreateSingleApprovalInstance()
        {
            // Arrange
            var contract = await CreateDraftContractAsync(
                fileUrl: "https://storage.qtkd.vn/concurrent.pdf",
                content: "Cộng hòa xã hội chủ nghĩa Việt Nam - Concurrent test");

            // Act: Gửi 2 request đồng thời (không dùng cùng key để kiểm tra tranh chấp thực tế)
            var task1 = _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", $"KEY-A-{Guid.NewGuid():N}"));
            var task2 = _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", $"KEY-B-{Guid.NewGuid():N}"));
            var responses = await Task.WhenAll(task1, task2);

            // Assert: Đúng 1 request thành công (200 OK) và 1 request bị từ chối xung đột (409 Conflict)
            int okCount = 0;
            int conflictCount = 0;
            foreach (var r in responses)
            {
                if (r.StatusCode == HttpStatusCode.OK) okCount++;
                if (r.StatusCode == HttpStatusCode.Conflict) conflictCount++;
            }

            Assert.Equal(1, okCount);
            Assert.Equal(1, conflictCount);

            // Assert DB: Trạng thái là PendingApproval và chỉ có 1 CurrentApprovalId
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.PendingApproval, inDb!.Status);
            Assert.False(string.IsNullOrWhiteSpace(inDb.CurrentApprovalId));
        }

        [Fact(DisplayName = "AC01 (Slide 20 TC01) - Người không có quyền Sales gửi trình duyệt bị từ chối 403")]
        public async Task AC01_SubmitContract_WithoutSalesRole_ShouldReturn403_AndKeepDraft()
        {
            // Arrange
            var contract = await CreateDraftContractAsync(
                fileUrl: "https://storage.qtkd.vn/ac01.pdf",
                content: "Cộng hòa xã hội chủ nghĩa Việt Nam - AC01 test");

            // Act
            var response = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Guest"));

            // Assert HTTP Status + DB
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.Draft, inDb!.Status);
        }

        [Fact(DisplayName = "Pricing API - Tạo PriceList và kiểm tra đơn giá PriceItem hợp lệ / âm")]
        public async Task PricingApi_CreatePriceListAndItems_ShouldValidateUnitPrice()
        {
            // 1. Tạo PriceList mới
            var plRes = await _client.PostAsJsonAsync("/api/prices", new CreatePriceListDto
            {
                Name = $"Bảng giá Test {Guid.NewGuid():N}"[..20],
                EffectiveDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            Assert.Equal(HttpStatusCode.Created, plRes.StatusCode);
            var priceList = await plRes.Content.ReadFromJsonAsync<PriceList>();
            Assert.NotNull(priceList);

            // 2. Thêm PriceItem đơn giá âm -> 400 Bad Request
            var badItemRes = await _client.PostAsJsonAsync($"/api/prices/{priceList!.Id}/items", new CreatePriceItemDto
            {
                ServiceCode = "SV_ERR",
                UnitPrice = -1000
            });
            Assert.Equal(HttpStatusCode.BadRequest, badItemRes.StatusCode);

            // 3. Thêm PriceItem hợp lệ -> 200 OK và đọc lại từ DB
            var goodItemRes = await _client.PostAsJsonAsync($"/api/prices/{priceList.Id}/items", new CreatePriceItemDto
            {
                ServiceCode = "SV_PORT",
                UnitPrice = 250000
            });
            Assert.Equal(HttpStatusCode.OK, goodItemRes.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var itemInDb = await db.PriceItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.PriceListId == priceList.Id && i.ServiceCode == "SV_PORT");
            Assert.NotNull(itemInDb);
            Assert.Equal(250000m, itemInDb!.UnitPrice);
        }

        [Fact(DisplayName = "Week06 Demo E2E - Toàn bộ luồng Tạo Customer -> Draft -> Submit lỗi -> Sửa -> Submit thành công")]
        public async Task Week06DemoFlow_CustomerToSubmit_ShouldPassEndToEnd()
        {
            // 1. Tạo Customer hợp lệ
            var custRes = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerDto
            {
                Name = "Công ty TNHH Minh Phát (Demo E2E)",
                Status = "Active"
            }, JsonOptions);
            Assert.Equal(HttpStatusCode.Created, custRes.StatusCode);
            var customer = await custRes.Content.ReadFromJsonAsync<Customer>(JsonOptions);
            Assert.NotNull(customer);

            // 2. Tạo Contract Draft (thiếu FileUrl và ContractContent)
            var createContractRes = await _client.PostAsJsonAsync("/api/contracts", new CreateContractDto
            {
                ContractNo = $"HD-DEMO-{Guid.NewGuid():N}"[..16],
                CustomerId = customer!.Id,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FileUrl = null,
                ContractContent = null
            }, JsonOptions);
            Assert.Equal(HttpStatusCode.Created, createContractRes.StatusCode);
            var contract = await createContractRes.Content.ReadFromJsonAsync<Contract>(JsonOptions);
            Assert.NotNull(contract);
            Assert.Equal(ContractStatus.Draft, contract!.Status);

            // 3. Submit khi thiếu dữ liệu -> Thất bại 400 (CTR-02)
            var failSubmitRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales"));
            Assert.Equal(HttpStatusCode.BadRequest, failSubmitRes.StatusCode);

            // 4. Bổ sung FileUrl & Quốc hiệu (CTR-01 cho phép sửa Draft)
            var updateRes = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", new UpdateContractDto
            {
                FileUrl = "https://storage.qtkd.vn/contracts/HD-2026-018.pdf",
                ContractContent = "Cộng hòa xã hội chủ nghĩa Việt Nam - Hợp đồng dịch vụ cảng biển"
            }, JsonOptions);
            Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

            // 5. Submit lại khi đủ dữ liệu -> Thành công 200 (CTR-03, AC04)
            var idemKey = $"KEY-DEMO-{Guid.NewGuid():N}";
            var successRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", idemKey));
            Assert.Equal(HttpStatusCode.OK, successRes.StatusCode);
            var submitResult = await successRes.Content.ReadFromJsonAsync<SubmitContractResponseDto>(JsonOptions);
            Assert.NotNull(submitResult);
            Assert.Equal("PendingApproval", submitResult!.Status);

            // 6. Đọc lại qua API GET và đối chiếu trực tiếp DB
            var getRes = await _client.GetAsync($"/api/contracts/{contract.Id}");
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
            var contractFromApi = await getRes.Content.ReadFromJsonAsync<Contract>(JsonOptions);
            Assert.Equal(ContractStatus.PendingApproval, contractFromApi!.Status);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var inDb = await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contract.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ContractStatus.PendingApproval, inDb!.Status);
            Assert.Equal(submitResult.ApprovalId, inDb.CurrentApprovalId);

            // 7. Submit lặp cùng Idempotency-Key -> trả cùng ApprovalId (AC05); sửa sau Submit -> 409 (CTR-01)
            var repeatRes = await _client.SendAsync(BuildSubmitRequest(contract.Id, "Sales", idemKey));
            Assert.Equal(HttpStatusCode.OK, repeatRes.StatusCode);
            var repeatResult = await repeatRes.Content.ReadFromJsonAsync<SubmitContractResponseDto>(JsonOptions);
            Assert.Equal(submitResult.ApprovalId, repeatResult!.ApprovalId);

            var editAfterSubmitRes = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", new UpdateContractDto
            {
                ContractNo = "HD-SUA-SAU-SUBMIT"
            }, JsonOptions);
            Assert.Equal(HttpStatusCode.Conflict, editAfterSubmitRes.StatusCode);
        }
    }
}
