using System;
using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs
{
    public class CreatePriceListDto
    {
        [Required(ErrorMessage = "Tên bảng giá không được để trống.")]
        public string Name { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    }

    public class CreatePriceItemDto
    {
        [Required(ErrorMessage = "Mã dịch vụ không được để trống.")]
        public string ServiceCode { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0.")]
        public decimal UnitPrice { get; set; }
    }
}
