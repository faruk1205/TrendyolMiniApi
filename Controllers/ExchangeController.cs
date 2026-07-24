// Controllers/ExchangeController.cs
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Providers;

namespace TrendyolMiniApi.Controllers
{
    public class ExchangeController : BaseApiController
    {
        // DİKKAT: Artık tek bir nesne değil, Provider LİSTESİ alıyoruz
        private readonly IEnumerable<IExchangeRateProvider> _providers;
        
        // URL'den hangi tipi istediğimizi parametre olarak alıyoruz (rest veya soap)
        [HttpGet("usd-to-try")]
        public async Task<IActionResult> GetUsdToTryRate([FromQuery] string type = "REST")
        {
            // İstemci "SOAP" gönderdiyse listeden SOAP olanı, "REST" gönderdiyse REST olanı bul!
            var selectedProvider = _providers.FirstOrDefault(p =>
                p.ProviderType.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (selectedProvider == null)
            {
                return BadRequest("Geçersiz sağlayıcı tipi. 'REST' veya 'SOAP' gönderin.");
            }

            // Seçtiğimiz provider hangisiyse, onun içindeki metot çalışacak!
            decimal currentRate = await selectedProvider.GetTryExchangeRateAsync("USD");

            return Ok(new
            {
                Provider = selectedProvider.ProviderType,
                Rate = currentRate
            });
        }
    }
}