namespace QTKD.Api.DTOs
{
    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class UpdateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }
}
