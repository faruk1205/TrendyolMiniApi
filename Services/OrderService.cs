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

        public async Task<int> CreateOrderAsync(OrderCreateDto request, int customerId)
        {
            int maxRetryCount = 3; // Maksimum 3 kez deneyeceğiz

            for (int attempt = 3; attempt > 0 ; attempt--)
            {
                try
                {
                    var product = await _context.Products.FindAsync(request.ProductId);

                    if (product == null)
                        throw new KeyNotFoundException("Sipariş vermek istediğiniz ürün bulunamadı.");

                    if (product.Stock < request.Quantity)
                        throw new InvalidOperationException(
                            $"Yetersiz stok! Bu üründen sadece {product.Stock} adet kaldı.");

                    // ---------------------------------------------------------
                    // 2. DOĞRUDAN SOAP API KULLANIMI
                    // İstemciyi ayağa kaldırıyoruz
                    using var soapClient =
                        new CalculatorSoapClient(CalculatorSoapClient.EndpointConfiguration.CalculatorSoap);

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
                    
                    product.Stock -= request.Quantity; // 4. Stoktan düş

                    await _context.SaveChangesAsync(); // 5. Kaydet
                    
                    return order.Id;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // YARIŞ DURUMU YAKALANDI! (Stok 100 iken iki kişi aynı anda aldı)

                    // Eğer 3. denemede de başaramadıysak artık pes et.
                    if (attempt == maxRetryCount)
                    {
                        throw new InvalidOperationException(
                            $"Sistemde anlık bir yoğunluk var, işleminizi gerçekleştiremedik. {attempt-1} kez denenecek.");
                    }

                    // SİHİR BURADA: EF Core'un hafızasındaki bayat veriyi (100'ü) silip, 
                    // veritabanındaki güncel veriyi (99'u) çekmesini sağlıyoruz.
                    foreach (var entry in ex.Entries)
                    {
                        await entry.ReloadAsync();
                    }
                }
            } // Döngü başa sarar ve 2. deneme (attempt = 2) başlar...//Bizim Global Exception Handler'ımız (merkezi hata yakalayıcı) bir hata fırlatıldığında ne yapacağını bilir,
            return 0;
        }
        /*ex.Entries nedir? DbUpdateConcurrencyException oluştuğunda EF Core sana hangi entity(ler) üzerinde concurrency hatası oluştuğunu verir.
        Burada hata Product üzerinde oluştuysa ex.Entries içinde Product entity'sinin takip edilen (tracked) hali bulunur.
        Buradaki entry, aslında bir EntityEntry nesnesidir. EF Core'un o entity hakkında tuttuğu bilgileri içerir:
        entiynin kendisi,eski değerleri,yen, değerleri,değişen alanları, entitynin durumu (added,modified,deleted...)
        ReloadAsync() ne yapıyor? diyelimki veritabanında biri alış yaptığı için stok azaladı ama ben veriyi önceden RAM'e çektiğim için eski değeri kaldı elimde. ReloadAsync() sayesinde takip ettiğimiz entity
        değerlerini tekrardan çekip rami güncelliyoruz.ZATEN HER FOR'DA VERİ ÇEKİLİYOR NE GEREK VAR DERSEN DE:
         ReloadAsync() gereksiz değil. Çünkü FindAsync() aynı DbContext içinde önce Change Tracker'a bakar(YANİ BU ENTİTY ZATEN BENDE VAR DER VE SQL BİLE ÇALIŞTIRMAZ;
          entity zaten takip ediliyorsa veritabanına gitmez. ReloadAsync() bu takip edilen entity'nin değerlerini veritabanındaki güncel değerlerle yeniler, böylece sonraki iterasyonda FindAsync() güncel veriyi döndürür.
          !!!DbUpdateConcurrencyException stok yetersiz olduğu için fırlamaz. Sadece başka bir işlem senin okuduğun veriyi sen kaydetmeden önce değiştirmişse fırlar.
          */
        

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