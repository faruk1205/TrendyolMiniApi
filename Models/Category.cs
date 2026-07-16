namespace TrendyolMiniApi.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        // İlişkiler
        public List<Product> Products { get; set; } = new();
    }
}