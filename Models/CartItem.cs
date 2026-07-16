namespace TrendyolMiniApi.Models
{
    public class CartItem : BaseEntity
    {
        // Hangi müşterinin sepetinde?
        public int UserId { get; set; }
        public User? User { get; set; }
        
        // Sepetteki ürün hangisi?
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        // O üründen sepete kaç tane eklendi?
        public int Quantity { get; set; }
    }
}