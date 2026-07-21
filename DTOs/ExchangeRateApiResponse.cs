namespace TrendyolMiniApi.DTOs
{
    public class ExchangeRateApiResponse
    {
        public string Base { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}