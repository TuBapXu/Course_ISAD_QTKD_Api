using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Tests.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class CustomerIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public CustomerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "TC01 - Tạo Customer hợp lệ phải trả về 201 và lưu vào DB")]
        public async Task TC01_CreateValidCustomer_ShouldSucceed_AndReadBackFromDb()
        {
            // 1. Arrange: Chuẩn bị dữ liệu hợp lệ
            var request = new CreateCustomerDto
            {
                Name = "Công ty TNHH Kiểm Thử TC01",
                Status = "Active"
            };

            // 2. Act: Gọi API POST /api/customers
            var response = await _client.PostAsJsonAsync("/api/customers", request);

            // 3. Assert lớp 1: Kiểm tra HTTP Status Code (201 Created)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // 4. Assert lớp 2: Kiểm tra nội dung JSON trả về từ API
            var createdCustomer = await response.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(createdCustomer);
            Assert.True(createdCustomer!.Id > 0);
            Assert.Equal("Công ty TNHH Kiểm Thử TC01", createdCustomer.Name);
            Assert.Equal("Active", createdCustomer.Status);

            // 5. Assert lớp 3a: Đọc lại qua API GET /api/customers/{id}
            var getResponse = await _client.GetAsync($"/api/customers/{createdCustomer.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var customerFromApi = await getResponse.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customerFromApi);
            Assert.Equal(createdCustomer.Name, customerFromApi!.Name);

            // 6. Assert lớp 3b: Truy vấn trực tiếp database kiểm thử QtkdTestDb
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
            var customerInDb = await db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == createdCustomer.Id);

            Assert.NotNull(customerInDb);
            Assert.Equal("Công ty TNHH Kiểm Thử TC01", customerInDb!.Name);
            Assert.Equal("Active", customerInDb.Status);
        }

        [Fact(DisplayName = "TC02 - Tạo Customer thiếu thông tin bắt buộc bị từ chối 400 và có thông báo lỗi")]
        public async Task TC02_CreateCustomer_WhenNameIsEmpty_ShouldReturn400WithErrorMessage_AndNotSaveToDb()
        {
            // 1. Arrange: Đếm số Customer hiện có trong DB trước khi gọi API
            int countBefore;
            using (var scopeBefore = _factory.Services.CreateScope())
            {
                var dbBefore = scopeBefore.ServiceProvider.GetRequiredService<QtkdDbContext>();
                countBefore = await dbBefore.Customers.CountAsync();
            }

            var request = new CreateCustomerDto
            {
                Name = "   ", // Thiếu thông tin tên bắt buộc
                Status = "Active"
            };

            // 2. Act: Gọi API POST /api/customers
            var response = await _client.PostAsJsonAsync("/api/customers", request);

            // 3. Assert HTTP Status: 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // 4. Assert Response Body: Phải có thông báo lỗi rõ ràng
            var errorBody = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(errorBody);
            Assert.NotNull(errorBody!.Errors);
            Assert.Contains(errorBody.Errors!, e => e.Contains("Tên khách hàng không được để trống"));

            // 5. Assert Database: Số lượng Customer trong DB không tăng thêm
            using (var scopeAfter = _factory.Services.CreateScope())
            {
                var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<QtkdDbContext>();
                int countAfter = await dbAfter.Customers.CountAsync();
                Assert.Equal(countBefore, countAfter);
            }
        }
    }

    public class ApiErrorResponse
    {
        public string? Error { get; set; }
        public List<string>? Errors { get; set; }
    }
}
