using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services
{
    public interface IExcelService : IScopedService
    {
        // Geriye kaç adet ürün eklendiğini veya hata mesajlarını dönebiliriz.
        Task<int> ImportProductsAsync(IFormFile file, int sellerId);
        Task<byte[]> ExportProductsAsync( int sellerId);

    }
}