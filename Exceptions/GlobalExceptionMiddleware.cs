using System.Net; // Thư viện chứa các mã HttpStatusCode
using System.Text.Json; // Thư viện để dịch dữ liệu thành JSON

namespace QTKD.Api.Exceptions
{
    // Lớp Middleware xử lý lỗi tập trung toàn cầu
    public class GlobalExceptionMiddleware
    {
        // Biến lưu trữ tác vụ tiếp theo trong đường ống (pipeline) của ASP.NET
        private readonly RequestDelegate _next;

        // Constructor: .NET tự động truyền request tiếp theo vào đây
        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next; // Gán vào biến nội bộ
        }

        // Hàm này tự động chạy mỗi khi có một Request gửi tới API
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Cho phép request đi xuyên qua màng lọc, tiến vào Controller và Service
                await _next(context);
            }
            catch (Exception ex)
            {
                // Nếu ở bên trong (Controller/Service) có lệnh throw exception, 
                // luồng chạy sẽ văng ra đây. Ta gọi hàm xử lý lỗi.
                await HandleExceptionAsync(context, ex);
            }
        }

        // Hàm phân tích và định dạng lỗi trả về cho Client
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Báo cho Client biết dữ liệu trả về sẽ là định dạng JSON
            context.Response.ContentType = "application/json";

            // Dùng cấu trúc switch để ánh xạ loại lỗi (Exception) sang Mã HTTP (StatusCode)
            context.Response.StatusCode = exception switch
            {
                // Nếu là lỗi nghiệp vụ (thiếu dữ liệu, sai quy tắc) -> Mã 400 (Bad Request)[cite: 31]
                BusinessRuleException => (int)HttpStatusCode.BadRequest,
                
                // Nếu là lỗi sai trạng thái (xung đột) -> Mã 409 (Conflict)[cite: 31]
                StateConflictException => (int)HttpStatusCode.Conflict,
                
                // Nếu là lỗi không tìm thấy (gọi DB không ra) -> Mã 404 (Not Found)
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                
                // Các lỗi hệ thống khác (lỗi code, rớt mạng...) -> Mã 500 (Internal Server Error)
                _ => (int)HttpStatusCode.InternalServerError
            };

            // Lấy nội dung câu thông báo lỗi (Ưu tiên lỗi gốc bên trong nếu có)
            var errorMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;

            // Đóng gói câu lỗi vào một object { error = "..." } và biến nó thành chuỗi JSON
            var result = JsonSerializer.Serialize(new { error = errorMessage });

            // Ghi chuỗi JSON này vào luồng trả về (Response) cho người dùng
            return context.Response.WriteAsync(result);
        }
    }
}