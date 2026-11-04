using System;

namespace QTKD.Api.Exceptions
{
    // Dùng để báo lỗi người dùng không có quyền thao tác (AC01 -> Mã HTTP 403 Forbidden)
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Người dùng không có quyền thực hiện thao tác này.") 
            : base(message)
        {
        }
    }
}
