using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    public class ProductsController : BaseApiController
    {
        private readonly IProductService _productService;
        private readonly CurrentUser _currentUser;

        public ProductsController(IProductService productService, CurrentUser currentUser)
        {
            _productService = productService;
            _currentUser = currentUser;
        }

        [HttpPost]
        [Authorize(Roles = "Satıcı")]
        // 1. IActionResult kalktı, doğrudan BaseResponseDto<int> dönüyoruz.
        public async Task<BaseResponseDto<int>> CreateProduct([FromForm] ProductCreateDto request)
        {
            int newProductId = await _productService.CreateProductAsync(request, _currentUser.Id);
            
            return BaseResponseDto<int>.SuccessResult(newProductId, "Ürün başarıyla vitrine eklendi!");        
        }

        [HttpGet]
        // 2. Sayfalamalı listeyi jenerik çerçeve ile dönüyoruz. Ok() sarmalayıcısı yok.
        public async Task<BaseResponseDto<ProductPagedResponseDto>> GetProducts([FromQuery] ProductQueryParameters query, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductsAsync(query, cancellationToken);
            
            return BaseResponseDto<ProductPagedResponseDto>.SuccessResult(result, "Ürünler listelendi.");        
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Satıcı")]
        // 3. EN BÜYÜK TEMİZLİK: Try-catch bloğu tamamen silindi! Sadece başarı senaryosu kaldı.
        public async Task<BaseResponseDto> DeleteProduct(int id)
        {
            // Eğer ürün yoksa veya yetki yoksa, servis 'throw new' diyecek ve GlobalExceptionHandler bunu halledecek.
            await _productService.DeleteProductAsync(id, _currentUser.Id);
            
            return BaseResponseDto.SuccessResult("Ürün başarıyla vitrinden kaldırıldı.");
        }

        [HttpGet("showcase")]
        // 4. Vitrin ürünleri için de tip güvenli dönüş.
        public async Task<BaseResponseDto<object>> GetShowcaseProducts(CancellationToken cancellationToken)
        {
            var showcaseData = await _productService.GetShowcaseProductsAsync(cancellationToken);

            return BaseResponseDto<object>.SuccessResult(showcaseData, "Vitrin ürünleri başarıyla getirildi.");
        }

        [HttpGet("{id}")]
        public async Task<BaseResponseDto<ProductResponseDto>> GetProductId(int id)
        {
            return BaseResponseDto<ProductResponseDto>.SuccessResult(await _productService.GetProductDetail(id));
        }
    }
}