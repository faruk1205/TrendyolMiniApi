namespace TrendyolMiniApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // İlişkiler
        public List<Product> Products { get; set; } = new();
    }
}