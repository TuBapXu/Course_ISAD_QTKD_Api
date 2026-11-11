using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;
using QTKD.Api.Services;
using DotNetEnv;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

Env.Load();
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// 1. Cấu hình DbContext kết nối PostgreSQL
builder.Services.AddDbContext<QtkdDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Đăng ký các Repository và Service (Dependency Injection)
builder.Services.AddScoped<IQtkdRepository, QtkdRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<QtkdQueryService>();

// 3. Đăng ký Controllers và cấu hình tuần tự hóa JSON
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Bỏ qua lỗi vòng lặp (Object Cycle) khi trả về JSON giữa các Entity liên kết
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // Chuyển Enum thành chuỗi khi serialize JSON trả về cho Client
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Middleware xử lý lỗi toàn cục (Global Exception Handler)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 5. Cấu hình HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 6. Khởi động ứng dụng
app.Run();

// Cho phép project QTKD.Tests khởi tạo WebApplicationFactory<Program>
public partial class Program { }
