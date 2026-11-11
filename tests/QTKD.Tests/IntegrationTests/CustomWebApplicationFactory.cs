using System;
using System.Linq;
using DotNetEnv;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QTKD.Api.Data;

namespace QTKD.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 1. Gỡ cấu hình DbContext của môi trường dev (QtkdDb)
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<QtkdDbContext>));
                if (dbContextDescriptor != null)
                    services.Remove(dbContextDescriptor);

                // Bắt buộc trên EF Core 10: gỡ luôn IDbContextOptionsConfiguration để tránh xung đột
                var dbConfigDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDbContextOptionsConfiguration<QtkdDbContext>));
                if (dbConfigDescriptor != null)
                    services.Remove(dbConfigDescriptor);

                // 2. Lấy chuỗi kết nối từ file .env và đổi tên DB sang QtkdTestDb
                Env.TraversePath().Load();
                var baseConn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
                    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                    ?? throw new InvalidOperationException("Không tìm thấy DB_CONNECTION_STRING trong .env");

                var connBuilder = new NpgsqlConnectionStringBuilder(baseConn)
                {
                    Database = "QtkdTestDb"
                };

                // 3. Đăng ký QtkdDbContext trỏ sang QtkdTestDb
                services.AddDbContext<QtkdDbContext>(options =>
                    options.UseNpgsql(connBuilder.ConnectionString));

                // 4. Tạo lại database test sạch sẽ và chạy Migration
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
                db.Database.EnsureDeleted();
                db.Database.Migrate();
            });
        }
    }
}