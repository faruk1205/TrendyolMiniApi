namespace TrendyolMiniApi.DTOs
{
    public class ProductQueryParameters
    {
        // Arama (Filtreleme)
        public string? Search { get; set; } // Örn: "iphone"
        public int? CategoryId { get; set; } // Örn: 1 (Elektronik)
        public decimal? MinPrice { get; set; } // Örn: 5000
        public decimal? MaxPrice { get; set; } // Örn: 20000
        
        // Sıralama
        public string? SortBy { get; set; } // "price_asc", "price_desc", "newest"
        
        // Sayfalama (Pagination)
        public int PageNumber { get; set; } = 1; // Varsayılan 1. sayfa
        public int PageSize { get; set; } = 10;  // Varsayılan sayfa başı 10 ürün
    }
}