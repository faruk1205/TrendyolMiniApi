using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Calculator;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class CartService : ICartService, IScopedService
    {
        private readonly ApplicationDbContext _context;
        private readonly CalculatorSoapClient _soapClient;
        private readonly IMapper _mapper ;


        public CartService(ApplicationDbContext context, CalculatorSoapClient soapClient, IMapper mapper)
        {
            _context = context;
            _soapClient = soapClient;
            _mapper = _mapper;
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
            // Include YAZMIYORUZ! AutoMapper DTO'da ProductName olduğunu görüp Include'u SQL'e kendi ekler.
            //Include(c => c.Product) deseydik. EF Core gidip, Product tablosundaki bütün sütunları (Description, ImagePath, CategoryId vs.) RAM'e çekerdi.
            //AutoMapper'ın .ProjectTo<T>() adında efsanevi bir metodu vardır. Bu metot, C# tarafında değil, doğrudan SQL sorgusu yazılırken araya girer ve EF Core'a "Sadece DTO'da eşleşen şu 4 sütunu getir, gerisini getirme" der.
            var cartItemDtos = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ProjectTo<CartItemResponseDto>(_mapper.ConfigurationProvider) // SQL'i DTO'ya göre filtrele!
                .ToListAsync();

            var totalCartAmount = cartItemDtos.Sum(c => c.Quantity);

            return new CartDetailResponseDto
            {
                Items = cartItemDtos,
                TotalAmount = totalCartAmount
            };
            
            /*YA DA:-----------------------------------
// 1. Veriyi veritabanından Liste (List<CartItem>) olarak çekiyoruz
    var cartItems = await _context.CartItems
        .Include(c => c.Product)
        .Where(c => c.UserId == userId)
        .ToListAsync(); // Select'i sildik!

    // 2. Toplam tutarı hesapla (Senin yazdığın gibi)
    var totalCartAmount = cartItems.Sum(c => c.Quantity);

    // 3. SİHİR BURADA: AutoMapper veritabanı modelini DTO'ya dönüştürüyor
    return new CartDetailResponseDto
    {
        Items = _mapper.Map<List<CartItemResponseDto>>(cartItems),
        TotalAmount = totalCartAmount
    };*/
            //autoMappingsiz hali------------------------------------------------
            
            /*var cartItems = await _context.CartItems
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

            var totalCartAmount = cartItems.Sum(c => c.Quantity);

            return new CartDetailResponseDto
            {
                Items = cartItems,
                TotalAmount = totalCartAmount
            };*/
        }

        public async Task<int> CheckoutAsync(int userId)
        {
            int maxRetryCount = 3; // Maksimum 3 kez deneyeceğiz

            for (int attempt = 3; attempt > 0; attempt--)
            {
                try
                {
                    // 1. Sepetteki ürünleri veritabanından çekiyoruz
                    var cartItems = await _context.CartItems
                        .Include(c => c.Product)
                        .Where(c => c.UserId == userId)
                        .ToListAsync();

                    if (!cartItems.Any())
                        throw new InvalidOperationException("Sepetiniz boş.");

                    // 2. Sipariş (Order) taslağını hazırlıyoruz
                    var order = new Order
                    {
                        UserId = userId,
                        CreatedDate = DateTime.UtcNow,
                        OrderItems = new List<OrderItem>()
                    };

                    decimal totalCartPrice = 0;
                    int totalQuantity = 0;

                    // 3. Sepetteki her ürün için stok kontrolü ve hesaplama yapıyoruz
                    foreach (var item in cartItems)
                    {
                        // Stok kontrolü
                        if (item.Product!.Stock < item.Quantity)
                            throw new InvalidOperationException(
                                $"'{item.Product.Name}' ürünü için yetersiz stok! Kalan: {item.Product.Stock}");

                        // SOAP API ile her kalemin fiyatını (Fiyat * Adet) hesaplıyoruz
                        var subTotal = await _soapClient.MultiplyAsync((int)item.Product.Price, item.Quantity);

                        totalCartPrice += subTotal;
                        totalQuantity += item.Quantity;

                        // Faturaya (OrderItem) kalemi ekliyoruz
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price
                        });

                        // DİKKAT: Ürünün stoğunu düşüyoruz
                        item.Product.Stock -= item.Quantity;
                    }

                    // Toplam tutarları ana faturaya yazıyoruz
                    order.TotalPrice = totalCartPrice; // SOAP'tan gelen toplam fiyat
                    order.TotalAmount = totalQuantity; // Toplam ürün adedi

                    // 4. Yeni siparişi veritabanına ekle ve Sepeti tamamen temizle
                    _context.Orders.Add(order);
                    _context.CartItems.RemoveRange(cartItems);

                    // 5. Kaydet ve Yarış Durumu (Concurrency) çakışması var mı dinle
                    await _context.SaveChangesAsync();

                    // Her şey başarılıysa döngüden çık ve Sipariş ID'sini dön
                    return order.Id;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // YARIŞ DURUMU YAKALANDI! Sepetteki ürünlerden birini tam o an başkası aldı.

                    // Son hakkımız da bittiyse pes et ve kullanıcıya hata dön.
                    if (attempt == 1)
                    {
                        throw new InvalidOperationException(
                            "Sistemde anlık bir yoğunluk var veya sepetinizdeki bazı ürünlerin stoğu tükendi. Lütfen tekrar deneyin.");
                    }

                    // Aksi halde çakışan ürünlerin stoğunu veritabanından tekrar (güncel haliyle) oku.
                    foreach (var entry in ex.Entries)
                    {
                        await entry.ReloadAsync();
                    }
                }
            }
            return 0;
        }
    }
}
//EntityEntry nedir?  EF Core, veritabanından çektiği her nesneyi takip eder. Arka planda EF bunun için bir EntityEntry oluşturur.Değişiklikleri takip eden şey EntityEntry değil, Change Tracker'dır.
//EntityEntry ise Change Tracker'ın tek bir entity için tuttuğu kayıtdır.
//DbUpdateConcurrencyException -> "Hayır, bu kayıt sen okuduktan sonra değişmiş." 

