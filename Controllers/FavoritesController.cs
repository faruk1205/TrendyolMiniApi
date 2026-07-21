using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize(Roles = "Müşteri")] 
    public class FavoritesController : BaseApiController
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        // 1. POST: Kusursuz! Doğrudan BaseResponseDto dönüyoruz.
        [HttpPost]
        public async Task<BaseResponseDto> AddToFavorites(FavoriteAddDto request)
        {
            await _favoriteService.AddToFavoritesAsync(request, CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Ürün favorilere eklendi!");
        }

        // 2. GET: IActionResult çöpe atıldı! Dönüş tipini açıkça Task<BaseResponseDto<List<...>>> yaptık.
        [HttpGet]
        public async Task<BaseResponseDto<List<ProductResponseDto>>> GetMyFavorites()
        {
            var favorites = await _favoriteService.GetMyFavoritesAsync(CurrentUserId);
            
            return BaseResponseDto<List<ProductResponseDto>>.SuccessResult(favorites, "Favoriler başarıyla listelendi.");
        }

        // 3. DELETE: IActionResult ve Ok() sarmalayıcısı kaldırıldı. Saf çerçeve dönüyoruz.
        [HttpDelete("{productId}")]
        public async Task<BaseResponseDto> RemoveFromFavorites(int productId)
        {
            await _favoriteService.RemoveFromFavoritesAsync(productId, CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Ürün favorilerden çıkarıldı.");
        }
    }
}