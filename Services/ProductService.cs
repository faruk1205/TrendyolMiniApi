using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly HybridCache _hybridCache;

        // Bütün araç gereçleri (Bağımlılıkları) Servisimize veriyoruz
        public ProductService(ApplicationDbContext context, IFileService fileService, HybridCache hybridCache)
        {
            _context = context;
            _fileService = fileService;
            _hybridCache = hybridCache;
        }

        public async Task<int> CreateProductAsync(ProductCreateDto request, int sellerId)
        {
            string imagePath = await _fileService.SaveImageAsync(request.Image, "products");

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

            return product.Id;
        }

        public async Task<ProductPagedResponseDto> GetProductsAsync(ProductQueryParameters query, CancellationToken cancellationToken)
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

        public async Task DeleteProductAsync(int id, int sellerId)
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
        }

        public async Task<object> GetShowcaseProductsAsync(CancellationToken cancellationToken)
        {
            var cacheKey = "Trendyol_Vitrin_EnYeniUrunler";

            return await _hybridCache.GetOrCreateAsync(
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
    }
}