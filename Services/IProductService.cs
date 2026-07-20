using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IProductService
    {
        Task<int> CreateProductAsync(ProductCreateDto request, int sellerId);
        Task<ProductPagedResponseDto> GetProductsAsync(ProductQueryParameters query, CancellationToken cancellationToken);
        Task DeleteProductAsync(int id, int sellerId);
        Task<object> GetShowcaseProductsAsync(CancellationToken cancellationToken);
    }
}