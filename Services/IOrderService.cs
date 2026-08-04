using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetMyOrdersAsync(int customerId);
    }
}