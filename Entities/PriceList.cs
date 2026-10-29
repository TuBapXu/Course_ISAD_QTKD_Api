namespace QTKD.Api.Entities
{
    // Lớp PriceList đại diện cho bảng danh mục Bảng giá
    public class PriceList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public List<PriceItem> PriceItems { get; set; } = new List<PriceItem>();
    }
}