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
        public string Status { get; set; } = "Draft"; 
        public Customer Customer { get; set; } = null!;
    }
}