// Controllers/ExchangeController.cs
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Providers;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : BaseApiController
    {
        private readonly IExchangeRateProvider _exchangeRateProvider;

        public ExchangeController(IExchangeRateProvider exchangeRateProvider)
        {
            _exchangeRateProvider = exchangeRateProvider;
        }

        [HttpGet("usd-to-try")]
        // Dikkat: IActionResult veya Ok() yok, doğrudan tipi dönüyoruz!
        public async Task<BaseResponseDto<decimal>> GetUsdToTryRate()
        {
            decimal currentRate = await _exchangeRateProvider.GetTryExchangeRateAsync("USD");

            return BaseResponseDto<decimal>.SuccessResult(currentRate, "Canlı dolar kuru başarıyla getirildi.");
        }
    }
}