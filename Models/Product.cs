namespace TrendyolMiniApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } // Parasal işlemlerde float/double yerine "decimal" kullanılır!
        public int Stock { get; set; }
        public string ImagePath { get; set; } = string.Empty;

        // Foreign Keys
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int SellerId { get; set; }
        public User? Seller { get; set; }

        // İlişkiler
        public List<Favorite> Favorites { get; set; } = new();
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}