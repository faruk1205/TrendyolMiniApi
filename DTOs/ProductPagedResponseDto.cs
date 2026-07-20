namespace TrendyolMiniApi.DTOs
{
    public class ProductPagedResponseDto
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<ProductResponseDto> Data { get; set; } = new();
    }
}