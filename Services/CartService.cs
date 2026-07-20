using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddToCartAsync(CartAddDto request, int userId)
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) 
                throw new KeyNotFoundException("Ürün bulunamadı.");

            if (product.Stock < request.Quantity)
                throw new InvalidOperationException($"Yetersiz stok! Sadece {product.Stock} adet kaldı.");

            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += request.Quantity;
                
                if (existingCartItem.Quantity > product.Stock)
                    throw new InvalidOperationException("Sepetteki toplam miktarınız depo stoğunu aşıyor.");
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<CartDetailResponseDto> GetMyCartAsync(int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new CartItemResponseDto
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product!.Name,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product.Price
                })
                .ToListAsync();

            var totalCartAmount = cartItems.Sum(c => c.SubTotal);

            return new CartDetailResponseDto
            {
                Items = cartItems,
                TotalAmount = totalCartAmount
            };
        }

        public async Task<int> CheckoutAsync(int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("Sepetiniz boş.");

            foreach (var item in cartItems)
            {
                if (item.Product!.Stock < item.Quantity)
                    throw new InvalidOperationException($"'{item.Product.Name}' ürünü için yetersiz stok! Kalan: {item.Product.Stock}");
            }

            var order = new Order
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                TotalAmount = cartItems.Sum(c => c.Quantity * c.Product!.Price),
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product!.Price // Zaman makinesi: O anki fiyat donduruldu
                });

                item.Product.Stock -= item.Quantity;
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return order.Id;
        }
    }
}