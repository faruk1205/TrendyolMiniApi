using System.ComponentModel.DataAnnotations;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TrendyolMiniApi.Calculator;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Services.Pdf;
using Order = TrendyolMiniApi.Models.Order;

namespace TrendyolMiniApi.Services
{
    public class CartService : ICartService, IScopedService
    {
        private readonly ApplicationDbContext _context;
        private readonly CalculatorSoapClient _soapClient;
        private readonly IMapper _mapper ;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly IDistributedLockService _lockService;
        private readonly IPdfService  _pdfService;
        private readonly IPdfTemplate<InvoiceDto>  _invoiceTemplate;

        public CartService(ApplicationDbContext context, CalculatorSoapClient soapClient, IMapper mapper,IConnectionMultiplexer redisConnection,IDistributedLockService lockService,IPdfService pdfService,IPdfTemplate<InvoiceDto> invoiceTemplate)
        {
            _context = context;
            _soapClient = soapClient;
            _mapper = mapper;
            _redisConnection = redisConnection;
            _lockService = lockService;
            _pdfService = pdfService;
            _invoiceTemplate = invoiceTemplate;
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

            #region MyRegion

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

            #endregion
           
        }

       public async Task<CheckoutResultDto> CheckoutAsync(int userId, CancellationToken ct = default)
{
    // 1. KULLANICIYI KONTROL ET
    var buyer = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (buyer == null) throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

    var cartItems = await _context.CartItems
        .Include(c => c.Product)
        .Where(c => c.UserId == userId)
        .ToListAsync(ct);

    if (!cartItems.Any()) throw new InvalidOperationException("Sepetiniz boş.");

    var lockKeys = cartItems.Select(c => $"product:{c.ProductId}");
    
    int newOrderId; // Kilit bloğundan çıkacak olan sipariş ID'si

    try
    {
        // 2. KİLİTLİ ALAN (Sadece hızlı DB işlemleri)
        newOrderId = await _lockService.ExecuteAsync(
            resourceKeys: lockKeys,
            ownerId: userId.ToString(),
            action: async () =>
            {
                var freshCartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync(ct);
                
                if (!freshCartItems.Any())
                    throw new InvalidOperationException("Sepetiniz boş.");

                var order = new Order
                {
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    OrderItems = new List<OrderItem>()
                };
                
                decimal totalCartPrice = 0;
                int totalQuantity = 0;

                foreach (var item in freshCartItems)
                {
                    if (item.Product!.Stock < item.Quantity)
                        throw new InvalidOperationException($"'{item.Product.Name}' ürünü için yetersiz stok! Kalan: {item.Product.Stock}");

                    // SOAP çağrısı
                    var subTotal = await _soapClient.MultiplyAsync((int)item.Product.Price, item.Quantity);
                    totalCartPrice += subTotal;
                    totalQuantity += item.Quantity;

                    order.OrderItems.Add(new OrderItem
                        { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.Product.Price });

                    item.Product.Stock -= item.Quantity;
                }

                order.TotalPrice = totalCartPrice;
                order.TotalAmount = totalQuantity;

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(freshCartItems); // Sepeti boşalt

                await _context.SaveChangesAsync(ct);
                return order.Id; 
            });
    }
    catch (DistributedLockAcquisitionException ex)
    {
        var productName = cartItems
            .FirstOrDefault(c => $"product:{c.ProductId}" == ex.ResourceKey)?.Product?.Name ?? "Ürün";
        throw new InvalidOperationException($"'{productName}' ürünü şu an başka bir müşteri tarafından satın alınıyor. Lütfen birazdan tekrar deneyin.");
    }

    // ---------------------------------------------------------
    // KİLİT AÇILDI! ARTIK DİĞER MÜŞTERİLER ALIŞVERİŞ YAPABİLİR.
    // ŞİMDİ AĞIR İŞ OLAN PDF ÜRETİMİNE GEÇEBİLİRİZ.
    // ---------------------------------------------------------

    // 3. FATURA DTO'SUNU HAZIRLA

    var invoiceDto = new InvoiceDto
    {
        OrderNumber = $"TRX-{newOrderId}-{DateTime.Now.Ticks.ToString()[^6..]}",
        CustomerFullName = $"{buyer.Username}",
        OrderDate = DateTime.Now,
        TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price),
        
        //  Select (LINQ) ile sepet verilerini yeni DTO'ya dönüştürüyoruz
        Items = cartItems.Select(c => new InvoiceItemDto
        {
            ProductName = c.Product.Name,
            Quantity = c.Quantity,
            UnitPrice = c.Product.Price
        }).ToList()
    };

    var pdfBytes = await _pdfService.GenerateAsync(invoiceDto, _invoiceTemplate);
    var fileName = $"Fatura_{invoiceDto.OrderNumber}.pdf";

    // EN SON AŞAMA: TUPLE YERİNE DTO DÖNDÜRÜYORUZ!
    return new CheckoutResultDto
    {
        OrderId = newOrderId,
        InvoicePdfBytes = pdfBytes,
        InvoiceFileName = fileName
    };
}
}
        /*Mantık: Bir müşteri X ürününü satın alma işlemine başladığında, Redis o ürünün üzerine sanal bir asma kilit asar.
         Başka bir müşteri aynı milisaniyede o ürünü almak isterse, Redis'e çarpar ve "Şu an başkası alıyor, bekle" yanıtını alır. 
         İşlem bitince kilit açılır. Buna yazılımda Distributed Lock (Dağıtık Kilit) denir. Bu mantığı servise taşıdık*/
    } 

//EntityEntry nedir?  EF Core, veritabanından çektiği her nesneyi takip eder. Arka planda EF bunun için bir EntityEntry oluşturur.Değişiklikleri takip eden şey EntityEntry değil, Change Tracker'dır.
//EntityEntry ise Change Tracker'ın tek bir entity için tuttuğu kayıtdır.
//DbUpdateConcurrencyException -> "Hayır, bu kayıt sen okuduktan sonra değişmiş." 

