namespace TrendyolMiniApi.DTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        
        // Resmin tam URL'sini veya yolunu tutacağız
        public string ImageUrl { get; set; } = string.Empty;
        
        // İlişkisel tablolardan (Join) çekeceğimiz güzel isimler
        public string CategoryName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
    }
}