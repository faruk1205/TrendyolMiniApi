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

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // 1. POST: IActionResult çöpe atıldı. Dönüş tipi int olarak eşitlendi.
        [HttpPost]
        public async Task<BaseResponseDto<int>> CreateOrder(OrderCreateDto request)
        {
            int orderId = await _orderService.CreateOrderAsync(request, CurrentUserId);
            
            // Ok() yok, doğrudan int taşıyan BaseResponseDto fırlatıyoruz.
            return BaseResponseDto<int>.SuccessResult(orderId, "Siparişiniz başarıyla alındı!");
        }
        
        // 2. GET: ActionResult ve Ok() kaldırıldı. Sadece standart çerçevemiz dönüyor.
        [HttpGet]
        public async Task<BaseResponseDto<List<OrderResponseDto>>> GetMyOrders()
        {
            var orders = await _orderService.GetMyOrdersAsync(CurrentUserId);
            
            return BaseResponseDto<List<OrderResponseDto>>.SuccessResult(orders, "Siparişleriniz başarıyla getirildi.");
        }
    }
}