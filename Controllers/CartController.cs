using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.Attributes;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize(Roles = "Müşteri")] // Sadece müşteriler sepet kullanabilir
    public class CartController : BaseApiController
    {
        private readonly ICartService _cartService;
        private readonly CurrentUser _currentUser;

        public CartController(ICartService cartService, CurrentUser currentUser)
        {
            _cartService = cartService;
            _currentUser = currentUser;
        }

        // 1. POST: Ürün ekleme. Sadece standart çerçeve döner. Ok() sarmalayıcısı yok.
        [HttpPost]
        public async Task<BaseResponseDto> AddToCart(CartAddDto request)
        {
            await _cartService.AddToCartAsync(request, _currentUser.Id);
            
            return BaseResponseDto.SuccessResult("Ürün sepetinize eklendi.");
        }

        // 2. GET: Sepet detaylarını listeler. Dönüş tipini Task<BaseResponseDto<...>> olarak açıkça belirtiyoruz.
        [HttpGet]
        public async Task<BaseResponseDto<CartDetailResponseDto>> GetMyCart()
        {
            var cart = await _cartService.GetMyCartAsync(_currentUser.Id);
            
            return BaseResponseDto<CartDetailResponseDto>.SuccessResult(cart, "Sepet başarıyla getirildi.");
        }

        [Idempotent]
        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout(CancellationToken ct)
        {
            // DTO nesnesini servisten teslim alıyoruz
            var result = await _cartService.CheckoutAsync(_currentUser.Id, ct);
    
            // DTO'nun içindeki dosya bilgilerini kullanarak PDF'i tarayıcıya indiriyoruz
            return File(result.InvoicePdfBytes, "application/pdf", result.InvoiceFileName);
        }
    }
}