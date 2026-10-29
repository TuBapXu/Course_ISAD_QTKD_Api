namespace QTKD.Api.Exceptions
{
    // Lớp này kế thừa từ lớp Exception gốc của C#
    // Chuyên dùng để báo lỗi vi phạm quy tắc nghiệp vụ (ví dụ: Không đạt chuẩn CTR-02)
    public class BusinessRuleException : Exception
    {
        // Constructor nhận vào một chuỗi thông báo lỗi (message)
        // và truyền ngược chuỗi đó (base) lên cho lớp cha Exception xử lý
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}