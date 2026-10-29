namespace QTKD.Api.Exceptions
{
    // Lớp này chuyên dùng để báo lỗi xung đột trạng thái 
    // (ví dụ: Vi phạm CTR-01, Hợp đồng đang duyệt mà đòi sửa)
    public class StateConflictException : Exception
    {
        // Nhận thông báo lỗi từ tầng Service và truyền lên lớp cha
        public StateConflictException(string message) : base(message)
        {
        }
    }
}