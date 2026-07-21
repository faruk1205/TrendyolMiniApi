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

        // 1. POST: Ürün ekleme. Sadece standart çerçeve döner. Ok() sarmalayıcısı yok.
        [HttpPost]
        public async Task<BaseResponseDto> AddToCart(CartAddDto request)
        {
            await _cartService.AddToCartAsync(request, CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Ürün sepetinize eklendi.");
        }

        // 2. GET: Sepet detaylarını listeler. Dönüş tipini Task<BaseResponseDto<...>> olarak açıkça belirtiyoruz.
        [HttpGet]
        public async Task<BaseResponseDto<CartDetailResponseDto>> GetMyCart()
        {
            var cart = await _cartService.GetMyCartAsync(CurrentUserId);
            
            return BaseResponseDto<CartDetailResponseDto>.SuccessResult(cart, "Sepet başarıyla getirildi.");
        }

        // 3. POST (Checkout): Sepeti siparişe dönüştürür. Yeni sipariş numarasını (int) taşıyan çerçeveyi fırlatırız.
        [HttpPost("checkout")]
        public async Task<BaseResponseDto<int>> Checkout()
        {
            int orderId = await _cartService.CheckoutAsync(CurrentUserId);
            
            return BaseResponseDto<int>.SuccessResult(orderId, "Siparişiniz başarıyla alındı! Sepetiniz temizlendi.");
        }
    }
}