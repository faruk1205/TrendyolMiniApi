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

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderCreateDto request)
        {
            // Servis hata fırlatırsa akıllı GlobalExceptionHandler yakalayıp 400 veya 404 dönecek
            int orderId = await _orderService.CreateOrderAsync(request, CurrentUserId);
            return Ok(new { Message = "Siparişiniz başarıyla alındı!", OrderId = orderId });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            // Amelelik bitti, BaseApiController'dan gelen CurrentUserId doğrudan kullanılıyor!
            var orders = await _orderService.GetMyOrdersAsync(CurrentUserId);
            return Ok(orders);
        }
    }
}