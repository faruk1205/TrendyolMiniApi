using System.Xml.Linq; // XML okumak için gerekli kütüphane

namespace TrendyolMiniApi.Providers
{
    public class SoapExchangeRateProvider : IExchangeRateProvider
    {
        // Bunun kimliğini SOAP olarak belirliyoruz
        public string ProviderType => "SOAP";

        private readonly HttpClient _httpClient;

        public SoapExchangeRateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD")
        {
            // 1. Veriyi JSON olarak değil, düz metin (String) olarak indiriyoruz
            var xmlString = await _httpClient.GetStringAsync("kurlar/today.xml");

            // 2. Metni XML dokümanına çeviriyoruz
            var xmlDoc = XDocument.Parse(xmlString);

            // 3. XML içinde "CurrencyCode='USD'" olan düğümü (node) buluyoruz
            var usdNode = xmlDoc.Descendants("Currency")
                .FirstOrDefault(x => x.Attribute("CurrencyCode")?.Value == baseCurrency);

            if (usdNode != null)
            {
                // 4. İçindeki <BanknoteSelling> etiketinin değerini alıyoruz
                string kurMetni = usdNode.Element("BanknoteSelling")!.Value;

                // Türk tipi virgüllü sayıları koda uyarlamak için ufak bir temizlik
                kurMetni = kurMetni.Replace(".", ","); 

                return Convert.ToDecimal(kurMetni);
            }

            throw new Exception("SOAP/XML servisinden döviz kuru alınamadı.");
        }
    }
}
// rest'te.GetFromJsonAsync<ExchangeRateApiResponse>() --> "Bana bir şablon (DTO) ver. Ben gelen JSON metnindeki isimlerle, senin DTO'ndaki isimleri otomatik eşleştireyim ve sana hazır bir nesne vereyim." kullanmıştık.
// AMA
//Burda yani soap'ta: Devasa bir XML'i satır satır DTO'ya dönüştürmenin (Deserialization) sunucuyu yoracağını bildiğimiz için, .NET'in XDocument (LINQ to XML) özelliğini kullandık.