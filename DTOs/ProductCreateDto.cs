namespace TrendyolMiniApi.DTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        
        // YENİ: Dışarıdan gelecek fiziksel dosya (Resim)
        public IFormFile? Image { get; set; } 
    }
}