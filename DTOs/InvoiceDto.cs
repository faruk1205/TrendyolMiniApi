namespace TrendyolMiniApi.DTOs
{
    public class InvoiceDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerFullName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        
        // DEĞİŞEN KISIM BURASI: Artık string değil, detaylı nesne listesi tutuyoruz
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}