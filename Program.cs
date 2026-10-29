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

// 1. Truyền chuỗi kết nối vừa lấy vào DbContext
builder.Services.AddDbContext<QtkdDbContext>(options =>
    options.UseNpgsql(connectionString));
// 2. ĐĂNG KÝ CÁC DỊCH VỤ NGHIỆP VỤ VÀ DATA (DI)
builder.Services.AddScoped<IQtkdRepository, QtkdRepository>();
builder.Services.AddScoped<QtkdQueryService>();

// 3. ĐĂNG KÝ CONTROLLERS VÀ SWAGGER
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Bỏ qua lỗi vòng lặp (Object Cycle) khi trả về JSON
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. CẮM LƯỚI BẮT LỖI
app.UseMiddleware<GlobalExceptionMiddleware>();

// 5. CẤU HÌNH PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 6. KHỞI ĐỘNG SERVER
app.Run();