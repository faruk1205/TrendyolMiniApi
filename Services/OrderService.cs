using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Entities;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Calculator; // Doğru namespace eklendi

namespace TrendyolMiniApi.Services

{
    public class OrderService : IOrderService , IScopedService
    {
        private readonly ApplicationDbContext _context;
        
        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateOrderAsync(OrderCreateDto request, int customerId)
        {
            // 1. Ürünü bul ve Stoğu Kontrol Et
            var product = await _context.Products.FindAsync(request.ProductId);
            
            if (product == null)
                throw new KeyNotFoundException("Sipariş vermek istediğiniz ürün bulunamadı.");

            if (product.Stock < request.Quantity)
                throw new InvalidOperationException($"Yetersiz stok! Bu üründen sadece {product.Stock} adet kaldı.");

            // ---------------------------------------------------------
            // 2. DOĞRUDAN SOAP API KULLANIMI
            // İstemciyi ayağa kaldırıyoruz
            using var soapClient = new CalculatorSoapClient(CalculatorSoapClient.EndpointConfiguration.CalculatorSoap);
            
            // DİKKAT: Metot artık doğrudan int döndüğü için direkt değişkene atıyoruz.
            // Body veya MultiplyResult aramamıza gerek kalmadı.
            var hesaplananToplamTutar = await soapClient.MultiplyAsync((int)product.Price, request.Quantity);
            // ---------------------------------------------------------

            // 3. Siparişi ve Kalemini Tek Seferde Oluştur
            var order = new Order
            {
                UserId = customerId,
                CreatedDate = DateTime.UtcNow,
                TotalPrice = hesaplananToplamTutar, // SOAP'tan gelen sonucu buraya yazdık
                TotalAmount = request.Quantity,
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

            _context.Orders.Add(order);

            // 4. Stoktan düş
            product.Stock -= request.Quantity;

            // 5. Kaydet
            await _context.SaveChangesAsync();

            return order.Id;
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