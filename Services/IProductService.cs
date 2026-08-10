using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public interface IProductService
    {
        Task<int> CreateProductAsync(ProductCreateDto request, int sellerId);
        Task<ProductPagedResponseDto> GetProductsAsync(ProductQueryParameters query, CancellationToken cancellationToken);
        Task DeleteProductAsync(int id, int sellerId, bool isHardDelete = false);
        Task<object> GetShowcaseProductsAsync(CancellationToken cancellationToken);
        Task<ProductResponseDto> GetProductDetail(int id);
        Task<List<ProductResponseDto>> GetAllProductsIncludeDeletedAsync();

        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportProductsAsync(int sellerId, CancellationToken ct);

        Task<ImportResultDto<Product>> ImportProductsAsync(IFormFile file, int sellerId, CancellationToken ct);
    }
}