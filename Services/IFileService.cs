using Microsoft.AspNetCore.Http;

namespace TrendyolMiniApi.Services
{
    public interface IFileService
    {
        // Klasör adını dışarıdan alıyoruz ki yarın "users", "categories" için de kullanabilelim
        Task<string> SaveImageAsync(IFormFile image, string folderName); 
    }
}