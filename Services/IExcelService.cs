using Microsoft.AspNetCore.Http;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IExcelService : IScopedService
    {
        // Dışa Aktar (Export)
        Task<byte[]> ExportAsync<T>(
            IEnumerable<T> data,
            Dictionary<string, Func<T, object?>> columnMappings,
            string sheetName = "Sayfa1",
            CancellationToken cancellationToken = default);

        // İçe Aktar (Import)
        Task<ImportResultDto<T>> ImportAsync<T>(
            IFormFile file,
            Func<List<string>, T> mapFunc,
            int startRow = 2,
            CancellationToken cancellationToken = default);
    }
}