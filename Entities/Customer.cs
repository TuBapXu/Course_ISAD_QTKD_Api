using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QTKD.Api.Entities
{
    // Lớp Customer đại diện cho bảng khách hàng
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Mối quan hệ 1-N với Contract (JsonIgnore để JSON trả về gọn sạch, không lặp [null])
        [JsonIgnore]
        public List<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
