using System;
using System.Collections.Generic;

namespace QTKD.Api.Exceptions
{
    // Chuyên dùng để báo lỗi vi phạm quy tắc nghiệp vụ (ví dụ: Không đạt chuẩn CTR-02 -> 400 Bad Request)
    public class BusinessRuleException : Exception
    {
        public List<string> Errors { get; }

        // Hỗ trợ ném nhiều lỗi cùng lúc (dùng cho mảng 400 Bad Request theo CTR-02 và AC03)
        public BusinessRuleException(List<string> errors) : base("Không đáp ứng quy tắc nghiệp vụ")
        {
            Errors = errors;
        }

        // Hỗ trợ ném 1 lỗi
        public BusinessRuleException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }
    }
}
