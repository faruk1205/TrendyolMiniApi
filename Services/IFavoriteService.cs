using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IFavoriteService
    {
        Task AddToFavoritesAsync(FavoriteAddDto request, int userId);
        Task<List<ProductResponseDto>> GetMyFavoritesAsync(int userId);
        Task RemoveFromFavoritesAsync(int productId, int userId);
    }
}