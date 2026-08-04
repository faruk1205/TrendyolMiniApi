using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize(Roles = "Müşteri")] // Sadece müşteriler sipariş verebilir!
    public class OrdersController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly CurrentUser _currentUser;

        public OrdersController(IOrderService orderService, CurrentUser currentUser)
        {
            _orderService = orderService;
            _currentUser = currentUser;
        }
        
        // 2. GET: ActionResult ve Ok() kaldırıldı. Sadece standart çerçevemiz dönüyor.
        [HttpGet]
        public async Task<BaseResponseDto<List<OrderResponseDto>>> GetMyOrders()
        {
            var orders = await _orderService.GetMyOrdersAsync(_currentUser.Id);
            
            return BaseResponseDto<List<OrderResponseDto>>.SuccessResult(orders, "Siparişleriniz başarıyla getirildi.");
        }
    }
}