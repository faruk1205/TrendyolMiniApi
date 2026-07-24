using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Providers
{
    public class RestExchangeRateProvider : IExchangeRateProvider
    {
        public string ProviderType => "REST";
        private readonly HttpClient _httpClient;

        // KİLİT NOKTA: HttpClient doğrudan .NET tarafından buraya enjekte ediliyor!
        public RestExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD")
        {
                // Base URL'i ayar dosyasında vereceğimiz için sadece bitiş noktasını yazıyoruz.
                // GetFromJsonAsync metodu, JSON'ı otomatik olarak bizim DTO'muza dönüştürür.
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateApiResponse>($"latest/{baseCurrency}");
                //var otomatik tip belirlemedir. Derleyici "GetFromJsonAsync<ExchangeRateApiResponse>()" görür ve der ki response "tipi ExchangeRateApiResponse" olacak. Yani aslında "ExchangeRateApiResponse response =" ile aynıdır.
                //"GetFromJsonAsync" ile apiden json veri çekilir->HttpClient bu json'ı string olarak alır->Bu json, ExchangeRateApiResponse nesnesine deserialize edilir.
                // .GetFromJsonAsync<ExchangeRateApiResponse>() --> "Bana bir şablon (DTO) ver. Ben gelen JSON metnindeki isimlerle, senin DTO'ndaki isimleri otomatik eşleştireyim ve sana hazır bir nesne vereyim."
            if (response != null && response.Rates.TryGetValue("TRY", out decimal tryRate)) 
            {
                return tryRate;
            }
            //"TryGetValue" bir Dictionary metodudur.Şunu yapar: TRY var mı? varsa değerini getir yoksa false dön.
            //"out" kelimesi, metodun sana ek bir değer döndürmesini sağlar. Yani "TRY = 39.45" ise şu olur "decimal tryRate = 39.45M" ve aynı zamanda true döner.
            //Yani "if(response.Rates.TryGetValue("TRY", out decimal tryRate))" --> "Eğer TRY anahtarı varsa, değerini tryRate değişkenine koy." demektir.
            //Bulunan kuru "return tryRate" ile döndürüyoruz.
            
            // Eğer API çöktüyse veya kura ulaşılamadıysa kalkanımızın yakalayacağı bir hata fırlatıyoruz.
            throw new Exception("Dış sistemden döviz kuru alınamadı.");
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