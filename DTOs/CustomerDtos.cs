using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs
{
    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";
    }

    public class UpdateCustomerDto
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";
    }
}
