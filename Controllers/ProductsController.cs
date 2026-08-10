using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;
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
        public async Task<BaseResponseDto> DeleteProduct(int id,[FromQuery] bool isHardDelete = false)
        {
            // Eğer ürün yoksa veya yetki yoksa, servis 'throw new' diyecek ve GlobalExceptionHandler bunu halledecek.
            await _productService.DeleteProductAsync(id, _currentUser.Id, isHardDelete);
            
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
        
        [HttpGet("AllwithSoftDeleted")]
        public async Task<BaseResponseDto<List<ProductResponseDto>>> GetAllProductsIncludeDeletedAsync()
        {
            return BaseResponseDto<List<ProductResponseDto>>.SuccessResult(await _productService.GetAllProductsIncludeDeletedAsync());
        }
        
        
        [HttpGet("export-excel")]
        [Authorize(Roles = "Satıcı")] 
        public async Task<IActionResult> ExportProductsToExcel(CancellationToken ct)
        {
            // 1. Tuple Deconstruction ile verileri yakala
            var (fileBytes, contentType, fileName) = await _productService.ExportProductsAsync(_currentUser.Id, ct); 
    
            return File(fileBytes, contentType, fileName);// 2. Dosyayı tarayıcıya fırlat!, File() metodu .NET'in ControllerBase sınıfından gelir. , Gelen byte dizisini alır ve tarayıcıya "Al bu bir dosyadır, indir" komutunu verir.
    
            #region MyRegion

            /* Eğer dönüş tipini BaseResponseDto<byte[]> yaparsan, API'ın bu yanıtı bir JSON nesnesine dönüştürür. Byte dizisi (Excel dosyasının 0 ve 1'leri) JSON formatına çevrilirken mecburen devasa bir Base64 metnine dönüşür.
       Kullanıcı "İndir" butonuna bastığında tarayıcıya Excel dosyası inmez; bunun yerine ekranda  devasa, anlamsız bir metin görür:
           {
              "success": true,
              "message": "İşlem başarılı",
              "data": "UEsDBBQABgAIAAAAIQCWpvwV1gEAABMGAAATAAgCW0NvbnRlbnRfVHlwZXNdLnhtbCCiBAIooAAC..."
          }
      Tarayıcının bir dosyayı "İndirilenler" klasörüne kaydedebilmesi için (Save As diyaloğu), sunucudan JSON değil, saf binary (ikili) veri ve özel HTTP başlıkları (MIME Type) gelmesi gerekir.
      Bu yüzden, projedeki tüm metotlar BaseResponseDto dönse bile, dosya fırlatan metotlar mecburen IActionResult (veya FileResult) dönmek zorundadır. Bu Clean Architecture'ı bozmaz, HTTP standartlarına uymanın bir gereğidir.
       
       Sen return File(...) dediğinde, .NET arka planda HTTP yanıtını (Response) şu şekilde yapılandırır ve tarayıcıya gönderir:
            content type: tarayıcıya bunun bir excel dosyası olduğunu söyler )
            Content-Disposition: Tarayıcıya dosyayı ekranda açmaya çalışma, doğrudan bilgisayara indir ve adını bu yap der. attachment; filename="Urun_Listesi_20260806.xlsx" böyle bir şey.
            Body: Saf bayt dizisi (0 ve 1'ler)
       */

            #endregion
        }

       
        [HttpPost("import-excel")]
        [Authorize(Roles = "Satıcı")]
        public async Task<BaseResponseDto<int>> ImportProductsFromExcel(IFormFile file, CancellationToken ct)
        {
            // Servis hata bulursa aşağı satıra inemeden Exception fırlatacak.
            var result = await _productService.ImportProductsAsync(file, _currentUser.Id, ct);

            // Buraya ulaşabildiysek işlem kesinlikle başarılıdır!
            return BaseResponseDto<int>.SuccessResult(
                result.Items.Count, 
                $"{result.Items.Count} adet ürün başarıyla eklendi.");
        }
        
    }
}