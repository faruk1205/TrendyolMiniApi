namespace TrendyolMiniApi.DTOs
{
    public class InvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Sadece okunabilir (Read-Only) özellik. 
        // Adet ile fiyatı çarpıp otomatik olarak satır toplamını verir.
        public decimal LineTotal => Quantity * UnitPrice; 
    }
}