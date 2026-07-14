namespace TrendyolMiniApi.DTOs
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}