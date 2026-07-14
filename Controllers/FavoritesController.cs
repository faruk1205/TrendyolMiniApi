using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Müşteri")] // GÜVENLİK DUVARI: Sadece müşteriler favorilere ürün ekleyebilir!
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. KALP İKONUNA BASMA (Favoriye Ekle)
        [HttpPost]
        public async Task<IActionResult> AddToFavorites(FavoriteAddDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Ürün veritabanında gerçekten var mı?
            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists) 
                return NotFound("Ürün bulunamadı.");

            // Müşteri bu ürünü zaten favorilemiş mi? (Aynı ürünü 2 kere eklemesini engelliyoruz)
            var alreadyFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.ProductId == request.ProductId);
            
            if (alreadyFavorite) 
                return BadRequest("Bu ürün zaten favorilerinizde ekli.");

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = request.ProductId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ürün favorilere eklendi!" });
        }

        // 2. FAVORİLERİM SAYFASI (Listele)
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Müşterinin favorilerini bul, içindeki ürün bilgilerini (Kategori ve Satıcı ile) çekip DTO'ya doldur
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Seller)
                .Select(f => new ProductResponseDto // Vitrinde kullandığımız çantayı tekrar kullanıyoruz!
                {
                    Id = f.Product!.Id,
                    Name = f.Product.Name,
                    Description = f.Product.Description,
                    Price = f.Product.Price,
                    Stock = f.Product.Stock,
                    ImageUrl = f.Product.ImagePath,
                    CategoryName = f.Product.Category != null ? f.Product.Category.Name : "Kategorisiz",
                    SellerName = f.Product.Seller != null ? f.Product.Seller.Username : "Bilinmeyen Satıcı"
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // 3. KALBİ GERİ ALMA (Favorilerden Çıkar)
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Favori kaydını bul
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null) 
                return NotFound("Bu ürün favorilerinizde bulunamadı.");

            // Kaydı veritabanından sil
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ürün favorilerden çıkarıldı." });
        }
    }
}