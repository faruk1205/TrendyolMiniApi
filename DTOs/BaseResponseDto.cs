namespace TrendyolMiniApi.DTOs
{
    // 1. Veri taşıyan standart yanıt çerçevesi (GET ve başarılı POST/PUT işlemleri için)
    public class BaseResponseDto<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        // Kolayca başarılı yanıt üretmek için yardımcı metotlar (Static Factory Methods)
        public static BaseResponseDto<T> SuccessResult(T data, string message = "İşlem başarılı.")
        {
            return new BaseResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }
    }
    // 2. Veri taşımayan (Sadece mesaj dönen) versiyonu (Örn: "Ürün silindi", "Şifre değiştirildi")
    public class BaseResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public static BaseResponseDto SuccessResult(string message)
        {
            return new BaseResponseDto
            {
                Success = true,
                Message = message
            };
        }
    }
}