using System;

namespace QTKD.Api.Entities
{
    // Lớp Contract đại diện cho bảng Hợp đồng
    public class Contract
    {
        public int Id { get; set; }
        public string ContractNo { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        
        // Các trường bổ sung phục vụ CTR-02 và US03
        public string? FileUrl { get; set; }
        public string? ContractContent { get; set; }
        
        // Mô phỏng phiên phê duyệt và Idempotency (AC04, AC05)
        public string? CurrentApprovalId { get; set; }
        public string? LastIdempotencyKey { get; set; }

        public Customer Customer { get; set; } = null!;
    }
}
