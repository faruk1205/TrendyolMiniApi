using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;

        public FavoriteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddToFavoritesAsync(FavoriteAddDto request, int userId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists) 
                throw new KeyNotFoundException("Ürün bulunamadı.");

            var alreadyFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.ProductId == request.ProductId);
            if (alreadyFavorite) 
                throw new InvalidOperationException("Bu ürün zaten favorilerinizde ekli.");

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = request.ProductId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductResponseDto>> GetMyFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Seller)
                .Select(f => new ProductResponseDto 
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
        }

        public async Task RemoveFromFavoritesAsync(int productId, int userId)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null) 
                throw new KeyNotFoundException("Bu ürün favorilerinizde bulunamadı.");

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}