using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Entities;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Calculator; // Doğru namespace eklendi

namespace TrendyolMiniApi.Services

{
    public class OrderService : IOrderService, IScopedService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.UserId == customerId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Select(o => new OrderResponseDto
                {
                    OrderId = o.Id,
                    OrderDate = o.CreatedDate,
                    TotalAmount = o.TotalAmount,
                    Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product != null ? oi.Product.Name : "Silinmiş Ürün",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}