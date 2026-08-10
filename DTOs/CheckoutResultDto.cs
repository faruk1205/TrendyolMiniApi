namespace TrendyolMiniApi.DTOs
{
    public class CheckoutResultDto
    {
        public int OrderId { get; set; }
        
        // PDF verilerini taşıyacak alanlar
        public byte[] InvoicePdfBytes { get; set; } = Array.Empty<byte>();
        public string InvoiceFileName { get; set; } = string.Empty;
        
        // Yarın buraya public string TrackingNumber { get; set; } bile ekleyebiliriz!
    }
}