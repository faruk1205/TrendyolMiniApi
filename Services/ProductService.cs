using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Extensions;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Services.Pdf;

namespace TrendyolMiniApi.Services
{
    public class ProductService : IProductService, IScopedService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly HybridCache _hybridCache;
        private readonly IMapper _mapper;
        private readonly IExcelService _excelService;
        

        // Bütün araç gereçleri (Bağımlılıkları) Servisimize veriyoruz
        public ProductService(ApplicationDbContext context, IFileService fileService, HybridCache hybridCache,
            IMapper mapper, IExcelService excelService)
        {
            _context = context;
            _fileService = fileService;
            _hybridCache = hybridCache;
            _mapper = mapper;
            _excelService = excelService ;
        }

        public async Task<int> CreateProductAsync(ProductCreateDto request, int sellerId)
        {
            // 1. Resmi sunucuya/buluta kaydet ve yolunu (path) al
            string imagePath = await _fileService.SaveImageAsync(request.Image, "products");

            // 2. SİHİR BURADA: DTO içindeki standart verileri (Name, Price, Stock vb.) otomatik eşleştir
            var product = _mapper.Map<Product>(request);

            // 3. EKSİKLERİ TAMAMLA: DTO'da olmayan, dışarıdan gelen özel verileri nesneye ekle
            product.SellerId = sellerId;
            product.ImagePath = imagePath;

            // 4. Veritabanına kaydet
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return product.Id;
            /*string imagePath = await _fileService.SaveImageAsync(request.Image, "products");
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                SellerId = sellerId, // Controller'dan parametre olarak geldi
                ImagePath = imagePath
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product.Id;*/
        }

        public async Task<ProductPagedResponseDto> GetProductsAsync(ProductQueryParameters query,
            CancellationToken cancellationToken)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm));
            }

            if (query.CategoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);

            if (query.MinPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price <= query.MaxPrice.Value);

            productsQuery = query.SortBy switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.Id),
                _ => productsQuery.OrderBy(p => p.Id)
            };

            var totalCount = await productsQuery.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            var products = await productsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrl = p.ImagePath,
                    CategoryName = p.Category != null ? p.Category.Name : "Kategorisiz",
                    SellerName = p.Seller != null ? p.Seller.Username : "Bilinmeyen Satıcı"
                })
                .ToListAsync(cancellationToken);

            return new ProductPagedResponseDto
            {
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = query.PageNumber,
                PageSize = query.PageSize,
                Data = products
            };
        }

        // isHardDelete parametresi varsayılan olarak false'tur. 
// Yani dışarıdan sadece (id, sellerId) gönderirsen Soft Delete çalışır.
        public async Task DeleteProductAsync(int id, int sellerId, bool isHardDelete = false)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                throw new KeyNotFoundException("Silinmek istenen ürün bulunamadı.");

            if (product.SellerId != sellerId)
                throw new UnauthorizedAccessException("Sadece kendi eklediğiniz ürünleri silebilirsiniz!");

            // Karar Mekanizması: Hard Delete mi, Soft Delete mi?
            if (isHardDelete)
            {
                // Geçtiğimiz derste DbContext'e kazandırdığımız özel VIP metodumuz
                _context.HardRemove(product);
            }
            else
            {
                // Normal Remove (Sistemin bunu yakalayıp IsDeleted = true yapacak)
                _context.Products.Remove(product);
            }

            // Seçim ne olursa olsun, son sözü SaveChanges söyler
            await _context.SaveChangesAsync();
        }

        // .ExecuteDeleteAsync();  RAM'deki ChangeTracker'ı tamamen görmezden gelir ve SQL'e anında "DELETE FROM Products WHERE Id=..." sorgusu atar!*/

        #region MyRegion

        /* public async Task DeleteProductAsync(int id, int sellerId)      //soft delete yokken böyle yapıyorduk
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Silinmek istenen ürün bulunamadı.");

            if (product.SellerId != sellerId)
                throw new UnauthorizedAccessException("Sadece kendi eklediğiniz ürünleri silebilirsiniz!");

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // İş kuralı hatasını fırlatıyoruz, Controller bunu yakalayacak!
                throw new InvalidOperationException("Bu ürün daha önce sipariş edildiği (faturası kesildiği) için sistemden tamamen silinemez! Sadece pasife alınabilir.");
            }
        }*/

        #endregion


        public async Task<object> GetShowcaseProductsAsync(CancellationToken cancellationToken)
        {
            var cacheKey = "Trendyol_Vitrin_EnYeniUrunler";

            return await
                _hybridCache
                    .GetOrCreateAsync( //GetOrCreateAsync(...): "Bu veriyi getir. Önbellekte varsa direkt ver, yoksa şu kod bloğunu (veritabanı sorgusunu) çalıştır, sonucunu önbelleğe kaydet ve bana ver."
                        cacheKey,
                        async cancel =>
                        {
                            var newestProducts = await _context.Products.ToListAsync(cancel);
                            return new
                            {
                                CacheSaati = DateTime.Now.ToString("HH:mm:ss.fff"),
                                Urunler = newestProducts
                            };
                        },
                        cancellationToken: cancellationToken
                    );
        }

        public async Task<ProductResponseDto> GetProductDetail(int ProductId)
        {
            var product = await _context.Products.FindAsync(ProductId);

            if (product == null)
                return null;

            // SİHİR BURADA: "product nesnesini al ve ProductResponseDto tipine dönüştür"
            return _mapper.Map<ProductResponseDto>(product);

            #region MyRegion

            /*var product = await _context.Products
                .Where(p => p.Id == ProductId)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrl = p.ImagePath,
                    CategoryName = p.Category != null ? p.Category.Name : "Kategorisiz",
                    SellerName = p.Seller.Username,
                }).FirstOrDefaultAsync();
            return product;*/


            #endregion
        }

        // Servis Katmanındaki Kusursuz Kullanım:
        public async Task<List<ProductResponseDto>> GetAllProductsIncludeDeletedAsync()
        {
            var allProductsIncludingDeleted = await _context.Products
                
                .IncludeSoftDeleted()
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    // ... diğer proplar
                })
                .ToListAsync();

            return allProductsIncludingDeleted;
        }
        
        // 1. EXPORT İŞLEMİ (Demet - Tuple dönüyoruz)
        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportProductsAsync(int sellerId, CancellationToken ct)
        {
            var products = await _context.Products.Where(p => p.SellerId == sellerId).ToListAsync(ct);

            var columns = new Dictionary<string, Func<Product, object?>>
            {
                { "Kayıt No", p => p.Id },
                { "Ürün Adı", p => p.Name },
                { "Stok", p => p.Stock },
                { "Fiyat", p => p.Price }
            };

            var fileBytes = await _excelService.ExportAsync(products, columns, "Ürün_Listesi", ct);
            
            return (fileBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"Urun_Listesi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // 2. IMPORT İŞLEMİ
       public async Task<ImportResultDto<Product>> ImportProductsAsync(IFormFile file, int sellerId, CancellationToken ct)
{
    // 1. Excel'deki ham veriyi okuyup liste haline getir (ExcelService işini yapsın)
    var result = await _excelService.ImportAsync(file, cells => new Product
    {
        Name = cells[0],
        CategoryId = int.Parse(cells[1]),
        Price = decimal.Parse(cells[2]),
        Stock = int.Parse(cells[3]),
        Description = cells.Count > 4 ? cells[4] : string.Empty,
        SellerId = sellerId
    }, startRow: 2, ct);

    if (!result.IsSuccess)
    {
        var errorMessages = string.Join(" | ", result.Errors.Select(e => $"Satır {e.RowNumber}: {e.Message}"));
        throw new InvalidOperationException($"Excel hataları: {errorMessages}");
    }

    // --- UPSERT (GÜNCELLE VEYA EKLE) MANTIĞI BAŞLIYOR ---

    // 2. Satıcının veritabanındaki mevcut ürünlerini TEK BİR SORGU ile RAM'e çek
    var existingProducts = await _context.Products
        .Where(p => p.SellerId == sellerId)
        .ToListAsync(ct);

    var productsToInsert = new List<Product>();

    // 3. Excel'den gelen her bir ürün için RAM'de kontrol yap
    foreach (var parsedProduct in result.Items)
    {
        // E-ticarette eşleştirme genelde 'Barkod' veya 'SKU' ile yapılır.
        // Şimdilik ürünün 'Adı'nı benzersiz anahtar olarak kabul ediyoruz.
        var existing = existingProducts.FirstOrDefault(p => 
            p.Name.Equals(parsedProduct.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            // GÜNCELLEME (Update)
            // Sadece değişebilecek alanları (Fiyat, Stok vb.) Excel'den gelenlerle eziyoruz.
            existing.Price = parsedProduct.Price;
            existing.Stock = parsedProduct.Stock;
            existing.Description = parsedProduct.Description;
            
            // Not: EF Core RAM'de izlediği(Tracking) bir nesnenin değiştiğini otomatik anlar. 
            // _context.Update(existing) dememize gerek yoktur!
        }
        else
        {
            // YENİ EKLEME (Insert)
            // Eğer ürün mevcut listede yoksa, yeni eklenecekler listesine atıyoruz.
            productsToInsert.Add(parsedProduct);
        }
    }

    // 4. Sadece YENİ olanları veritabanına ekle
    if (productsToInsert.Any())
    {
        await _context.Products.AddRangeAsync(productsToInsert, ct);
    }

    // 5. Değişiklikleri (Hem güncellenenler hem yeniler) tek seferde veritabanına yansıt!
    await _context.SaveChangesAsync(ct);

    return result;
}
       
    }
}