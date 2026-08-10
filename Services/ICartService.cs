using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface ICartService
    {
        Task AddToCartAsync(CartAddDto request, int userId);
        Task<CartDetailResponseDto> GetMyCartAsync(int userId);

        Task<CheckoutResultDto> CheckoutAsync(int userId,
            CancellationToken ct = default);
    }
}