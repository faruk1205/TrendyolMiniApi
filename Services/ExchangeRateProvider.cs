// Services/ExchangeRateProvider.cs
using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        private readonly HttpClient _httpClient;

        // KİLİT NOKTA: HttpClient doğrudan .NET tarafından buraya enjekte ediliyor!
        public ExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD")
        {
            // Base URL'i ayar dosyasında vereceğimiz için sadece bitiş noktasını yazıyoruz.
            // GetFromJsonAsync metodu, JSON'ı otomatik olarak bizim DTO'muza dönüştürür.
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateApiResponse>($"latest/{baseCurrency}");

            if (response != null && response.Rates.TryGetValue("TRY", out decimal tryRate))
            {
                return tryRate;
            }

            // Eğer API çöktüyse veya kura ulaşılamadıysa kalkanımızın yakalayacağı bir hata fırlatıyoruz.
            throw new Exception("Dış sistemden döviz kuru alınamadı.");
        }
    }
}