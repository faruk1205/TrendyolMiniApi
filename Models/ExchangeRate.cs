// Entities/ExchangeRate.cs
namespace TrendyolMiniApi.Entities
{
    public class ExchangeRate
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; } // Örn: "USD"
        public decimal Rate { get; set; }        // Örn: 32.45
        public DateTime LastUpdated { get; set; } // En son ne zaman çektik?
        public string ProviderName { get; set; } // Hangi API'den aldık? (REST mi SOAP mı)
    }
}