// Services/IExchangeRateProvider.cs
namespace TrendyolMiniApi.Providers
{
    public interface IExchangeRateProvider
    {
        Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD");
    }
}