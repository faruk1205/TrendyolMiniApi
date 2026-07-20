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

        [HttpPost]
        public async Task<IActionResult> AddToFavorites(FavoriteAddDto request)
        {
            
                await _favoriteService.AddToFavoritesAsync(request, CurrentUserId);
                return Ok(new { Message = "Ürün favorilere eklendi!" });
            
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var favorites = await _favoriteService.GetMyFavoritesAsync(CurrentUserId);
            return Ok(favorites);
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
                await _favoriteService.RemoveFromFavoritesAsync(productId, CurrentUserId);
                return Ok(new { Message = "Ürün favorilerden çıkarıldı." });
           
        }
    }
}
