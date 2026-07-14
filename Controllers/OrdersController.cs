using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Müşteri")] // GÜVENLİK DUVARI: Sadece müşteriler sipariş verebilir!
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderCreateDto request)
        {
            // 1. JWT (Bilet) içinden müşterinin ID'sini çekiyoruz
            var customerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerIdString))
                return Unauthorized("Müşteri kimliği bulunamadı.");
            
            int customerId = int.Parse(customerIdString);

            // 2. Ürünü bul ve Stoğu Kontrol Et
            var product = await _context.Products.FindAsync(request.ProductId);
            
            if (product == null)
                return NotFound("Sipariş vermek istediğiniz ürün bulunamadı.");

            if (product.Stock < request.Quantity)
                return BadRequest($"Yetersiz stok! Bu üründen sadece {product.Stock} adet kaldı.");

            // 3. Siparişi (Ana Fatura) ve Kalemini (Satırı) Tek Seferde Oluştur
            var order = new Order
            {
                UserId = customerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = product.Price * request.Quantity,
                
                // EF Core Sihri: İlişkili tabloya (OrderItems) veriyi aynı anda ekliyoruz!
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = request.Quantity,
                        UnitPrice = product.Price
                    }
                }
            };

            // 4. Yeni siparişi sisteme ekle
            _context.Orders.Add(order);

            // 5. Ürünün stoğunu müşterinin aldığı kadar düşür
            product.Stock -= request.Quantity;

            // 6. HER ŞEYİ TEK BİR HAMLEDE VERİTABANINA KAYDET
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Siparişiniz başarıyla alındı!", OrderId = order.Id });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            // 1. JWT (Bilet) içinden sisteme giriş yapmış müşterinin ID'sini alıyoruz
            var customerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerIdString))
                return Unauthorized("Müşteri kimliği bulunamadı.");

            int customerId = int.Parse(customerIdString);

            // 2. Müşterinin siparişlerini çekiyoruz (Include ile alt kırılımları ve ürün isimlerini de birleştiriyoruz)
            var orders = await _context.Orders
                .Where(o => o.UserId == customerId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Sipariş kaleminin içindeki ürün tablosuna da ulaşıyoruz!
                .Select(o => new OrderResponseDto
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
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

            return Ok(orders);
        }
    }
}