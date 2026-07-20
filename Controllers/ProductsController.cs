using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    public class ProductsController : BaseApiController
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        [Authorize(Roles = "Satıcı")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto request)
        {
            // Controller sadece kimliği okur ve servise işi devreder
            int newProductId = await _productService.CreateProductAsync(request, CurrentUserId);
            return Ok(new { Message = "Ürün başarıyla vitrine eklendi!", ProductId = newProductId });
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParameters query, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductsAsync(query, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Satıcı")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id, CurrentUserId);
                return Ok(new { Message = "Ürün başarıyla vitrinden kaldırıldı." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("showcase")]
        public async Task<IActionResult> GetShowcaseProducts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetShowcaseProductsAsync(cancellationToken);
            return Ok(result);
        }
    }
}