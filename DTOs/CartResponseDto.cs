namespace TrendyolMiniApi.DTOs
{
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => Quantity * UnitPrice; // Otomatik hesaplanan ara toplam
    }

    public class CartDetailResponseDto
    {
        public List<CartItemResponseDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }
}