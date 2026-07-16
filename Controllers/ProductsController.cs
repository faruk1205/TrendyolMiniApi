using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;
using Microsoft.EntityFrameworkCore;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env; // Dosyaları kaydedeceğimiz fiziksel yolu bulmak için gerekli
        }

        [HttpPost]
        [Authorize(Roles = "Satıcı")] // GÜVENLİK DUVARI: Sadece satıcı biletine sahip olanlar girebilir!
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto request)
        {
            // 1. JWT (Bilet) içinden giriş yapan satıcının ID'sini çekiyoruz
            var sellerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerIdString)) 
                return Unauthorized("Satıcı kimliği bulunamadı.");
            
            int sellerId = int.Parse(sellerIdString);
            
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(request.Image.FileName).ToLower();
            
            if (Array.IndexOf(allowedExtensions, extension) == -1)
            {
                return BadRequest(new { message = "Sadece .jpg, .jpeg, .png ve .gif dosyaları yüklenebilir." });
            }

            // 2. Resim Yükleme İşlemi
            string imagePath = string.Empty;
            if (request.Image != null && request.Image.Length > 0)
            {
                // Dosyanın kaydedileceği fiziksel klasör: wwwroot/uploads/products
                var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "products");
                
                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadsFolder)) 
                    Directory.CreateDirectory(uploadsFolder);

                // Benzersiz bir dosya adı oluştur (Aynı isimde iki resim yüklenirse çakışmasın)
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Dosyayı sunucuya kopyala
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(fileStream);
                }

                // Veritabanına kaydedilecek web yolu
                imagePath = $"/uploads/products/{uniqueFileName}";
            }

            // 3. Ürünü Hazırla ve Veritabanına Kaydet
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                SellerId = sellerId, // Token'dan çaldığımız ID'yi buraya yapıştırdık
                ImagePath = imagePath
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ürün başarıyla vitrine eklendi!", ProductId = product.Id });
        }
        
       // VİTRİN: Gelişmiş Arama, Filtreleme ve Sayfalama
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParameters query)
        {
            // 1. TEMEL SORGUNUN BAŞLANGICI (Henüz veritabanına gitmedik)
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .AsQueryable();

            // 2. FİLTRELEME İŞLEMLERİ
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // İsmin veya açıklamanın içinde kelime geçiyorsa (Büyük/küçük harf duyarsız)
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

            // 3. SIRALAMA İŞLEMLERİ
            productsQuery = query.SortBy switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.Id),
                _ => productsQuery.OrderBy(p => p.Id) // Varsayılan sıralama
            };

            // 4. SAYFALAMA (Pagination) İŞLEMLERİ
            // Toplam kaç ürün eşleştiğini bul (Frontend'e sayfa sayısını göstermek için)
            var totalCount = await productsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            // Sadece istenen sayfanın verilerini getir (Skip ve Take sihridir)
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
                .ToListAsync(); // Veritabanına VURDUĞUMUZ an burasıdır!

            // 5. SONUÇ ÇANTASI (Müşteriye veriyi ve sayfa bilgilerini dön)
            return Ok(new
            {
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = query.PageNumber,
                PageSize = query.PageSize,
                Data = products
            });
        }
        
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Satıcı")] // Sadece satıcılar silebilir
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // 1. JWT'den giren satıcının ID'sini al
            var sellerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerIdString))
                return Unauthorized("Satıcı kimliği bulunamadı.");
            
            int sellerId = int.Parse(sellerIdString);

            // 2. Ürünü bul
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Silinmek istenen ürün bulunamadı.");

            // 3. Güvenlik: Bu ürün gerçekten bu satıcıya mı ait?
            if (product.SellerId != sellerId)
                return Forbid("Sadece kendi eklediğiniz ürünleri silebilirsiniz!");

            // 4. Silmeyi Dene (Restrict kurallarına takılabilir!)
            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                // İsteğe bağlı: wwwroot içindeki fiziksel resmi de silebilirsin
                // var imagePath = Path.Combine(_env.ContentRootPath, "wwwroot", product.ImagePath.TrimStart('/'));
                // if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);

                return Ok(new { Message = "Ürün başarıyla vitrinden kaldırıldı." });
            }
            catch (DbUpdateException)
            {
                // İŞTE SİGORTAMIZ ÇALIŞTI!
                // Eğer ürün OrderItems tablosunda varsa PostgreSQL hata fırlatır ve kod buraya düşer.
                return BadRequest("Bu ürün daha önce sipariş edildiği (faturası kesildiği) için sistemden tamamen silinemez! Sadece pasife alınabilir.");
            }
        }
    }
}