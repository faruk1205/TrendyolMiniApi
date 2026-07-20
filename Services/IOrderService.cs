using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(OrderCreateDto request, int customerId);
        Task<List<OrderResponseDto>> GetMyOrdersAsync(int customerId);
    }
}