using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Providers
{
    public class RestJsonExchangeRateProvider : IExchangeRateProvider
    {
        public string ProviderType => "RestJson";
        private readonly HttpClient _httpClient;

        // KİLİT NOKTA: HttpClient doğrudan .NET tarafından buraya enjekte ediliyor!
        public RestJsonExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD")
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ExchangeRateApiResponse>(
                    $"latest?base={baseCurrency}");

                if (response?.Rates.TryGetValue("TRY", out var tryRate) == true)
                {
                    return tryRate;
                }

                throw new Exception($"'{baseCurrency}' için TRY kuru bulunamadı.");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Dış sistemden döviz kuru alınamadı: {ex.Message}", ex);
            }
        }
    }
}
//serialize: c# nesnesi -> json
//deserialize: json -> c# nesnesi


/* "GetFromJsonAsync<T>()" olmasaydı:
 
var httpResponse = await _httpClient.GetAsync("latest/USD")
httpResponse.EnsureSuccessStatusCode();
string json = await httpResponse.Content.ReadAsStringAsync();
var dto = JsonSerializer.Deserialize<ExchangeRateApiResponse>(json);

şeklinde tek tek yazmak zorunda kalırdık. */