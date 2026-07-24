using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using TrendyolMiniApi.Markers; // IWebHostEnvironment için gerekli

namespace TrendyolMiniApi.Services
{
    public class FileService : IFileService , IScopedService
    {
        private readonly IWebHostEnvironment _env;

        // Dosya yollarını bulmak için IWebHostEnvironment'ı enjekte ediyoruz
        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile image, string folderName)
        {
            // 1. Resim yoksa boş dön (Belki kullanıcı resimsiz ürün ekliyor)
            if (image == null || image.Length == 0)
                return string.Empty;

            // 2. Güvenlik Kontrolü
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                // Hatırladın mı? Bunu fırlattığımızda yazdığımız GlobalExceptionHandler yakalayıp 
                // müşteriye şık bir 400 Bad Request veya 500 dönecek!
                throw new Exception("Geçersiz dosya formatı. Sadece resim dosyaları yüklenebilir."); 
            }

            // 3. Klasör İşlemleri
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // 4. Kaydetme İşlemi
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // 5. Veritabanına yazılacak yolu dön
            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}