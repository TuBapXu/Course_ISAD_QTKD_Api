using System.Net;
using System.Text.Json;

namespace QTKD.Api.Exceptions
{
    // Lớp Middleware xử lý lỗi tập trung toàn cầu
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            object responseBody;

            switch (exception)
            {
                case BusinessRuleException businessRuleEx:
                    statusCode = (int)HttpStatusCode.BadRequest; // 400
                    responseBody = new
                    {
                        error = businessRuleEx.Message,
                        errors = businessRuleEx.Errors
                    };
                    break;

                case ForbiddenException forbiddenEx:
                    statusCode = (int)HttpStatusCode.Forbidden; // 403
                    responseBody = new
                    {
                        error = forbiddenEx.Message
                    };
                    break;

                case StateConflictException stateConflictEx:
                    statusCode = (int)HttpStatusCode.Conflict; // 409
                    responseBody = new
                    {
                        error = stateConflictEx.Message
                    };
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    statusCode = (int)HttpStatusCode.NotFound; // 404
                    responseBody = new
                    {
                        error = keyNotFoundEx.Message
                    };
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError; // 500
                    var errorMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;
                    responseBody = new
                    {
                        error = "Đã xảy ra lỗi hệ thống không mong muốn.",
                        details = errorMessage
                    };
                    break;
            }

            context.Response.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(responseBody);
            return context.Response.WriteAsync(json);
        }
    }
}
