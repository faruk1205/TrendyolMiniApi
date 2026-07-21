// Services/IExchangeRateProvider.cs
namespace TrendyolMiniApi.Services
{
    public interface IExchangeRateProvider
    {
        Task<decimal> GetTryExchangeRateAsync(string baseCurrency = "USD");
    }
}