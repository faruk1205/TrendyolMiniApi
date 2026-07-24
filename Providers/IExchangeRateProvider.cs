namespace TrendyolMiniApi.Providers
{
    public interface IExchangeRateProvider
    {
        // Bu özellik sınıfları birbirinden ayırmamızı sağlayacak ("REST" veya "SOAP" gibi)
        string ProviderType { get; } 
        
        Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD");
    }
}