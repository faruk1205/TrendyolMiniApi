using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize(Roles = "Müşteri")] // Sadece müşteriler sepet kullanabilir
    public class CartController : BaseApiController
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(CartAddDto request)
        {
            await _cartService.AddToCartAsync(request, CurrentUserId);
            return Ok(new { Message = "Ürün sepetinize eklendi." });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var cart = await _cartService.GetMyCartAsync(CurrentUserId);
            return Ok(cart);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            int orderId = await _cartService.CheckoutAsync(CurrentUserId);
            return Ok(new { Message = "Siparişiniz başarıyla alındı! Sepetiniz temizlendi.", OrderId = orderId });
        }
    }
}