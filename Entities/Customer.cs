namespace QTKD.Api.Entities
{
    // Lớp Customer đại diện cho bảng khách hàng
    public class Customer
    {
        public int Id {get; set;}
        public string Name {get; set;} = string.Empty;
        public string Status {get; set;} = "Active";

        // Mối quan hệ 1-N vói Contract
        public List<Contract> Contracts {get; set;} =  new List <Contract>();
    }
}