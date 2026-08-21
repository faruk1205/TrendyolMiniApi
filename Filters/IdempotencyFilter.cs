using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using StackExchange.Redis;
using System.Text.Json;
using TrendyolMiniApi.Attributes;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Filters
{
    public class IdempotencyFilter : IAsyncActionFilter, IScopedService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly CurrentUser _currentUser;
        private readonly ILogger<IdempotencyFilter> _logger;
        
        private static readonly TimeSpan ProcessingTtl = TimeSpan.FromSeconds(30); //Bir istek işlenirken Redis'te oluşturulan "Kilit"in ömrüdür. API'nizin maksimum yanıt süresinden uzun olmalıdır ki işlem bitmeden kilit açılmasın.

        private static readonly TimeSpan CompletedTtl = TimeSpan.FromHours(24);//İşlem başarıyla bittikten sonra, verilen yanıtın Redis'te ne kadar süre hatırlanacağını belirler.

        public IdempotencyFilter(IConnectionMultiplexer redis, CurrentUser currentUser, ILogger<IdempotencyFilter> logger)
        {
            _redis = redis;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        //ActionExecutingContext context -> ASP.NET Core'un mevcut request hakkında verdiği bilgiler. Buradan context.HttpContext ile http requeste ulaşabiliyorsun. örneğin context.HttpContext.Request.Headers ile http header'a ulaşıyorsun.
        // next kabaca filter'dan sonraki pipeline'a devam et,controller action'ını çalıştır demek
        {
            //İstek yapılan uç noktada (endpoint) [Idempotent] niteliği olup olmadığına bakar. Eğer bu nitelik yoksa, await next() diyerek hiçbir işlem yapmadan normal akışa izin verir.
            var hasAttribute = context.ActionDescriptor.EndpointMetadata
                .Any(m => m is IdempotentAttribute);  

            if (!hasAttribute)
            {
                await next();
                return;
            }
            
            // Idempotency-Key Kontrolü: İstemciden (Frontend/Mobil) gelen HTTP Header'ları arasında bu anahtarın olup olmadığını denetler. Yoksa, işlemi durdurup 400 BadRequest döner.
            //İstemcinin şöyle bir header göndermesi bekleniyor: Idempotency-Key: abc123
            if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader)
                || string.IsNullOrWhiteSpace(keyHeader))  
            {
                context.Result = new BadRequestObjectResult(
                    new BaseResponseDto { Success = false, Message = "Idempotency-Key header'ı zorunludur." });
                return;
                //Burada controller'ı çalıştırmıyorsun direkt HTTP 400 Bad Request dönüyorsun(header yoksa)
            }

            var redisKey = $"idem:{_currentUser.Id}:{keyHeader}";
            var db = _redis.GetDatabase();

            // Atomik "sadece key yoksa yaz" işlemi (Race condition önleyici)
            var claimed = await db.StringSetAsync(redisKey, "processing", ProcessingTtl, When.NotExists); //When.NotExists -> "Sadece bu key Redis'te yoksa yaz."

            if (!claimed) //"Bu idempotency key zaten başka biri tarafından alınmış." demektir.
            {
                var existing = await db.StringGetAsync(redisKey); //var olan değeri okuyorsun

                if (existing == "processing")
                {
                    context.Result = new ConflictObjectResult(
                        new BaseResponseDto { Success = false, Message = "Bu istek zaten işleniyor, lütfen birkaç saniye bekleyin." }); // HTTP starus 409 Conflict dönüyorsun
                    return;
                }

                //İşlem tamamlandıysa Redisteki JSON'u IdempotentResponseEnvelope nesnesine dönüştürüyorsun.
                var envelope = JsonSerializer.Deserialize<IdempotentResponseEnvelope>((string)existing!);
                
                // Geri dönen veride gövde (Body) yoksa sadece statü kodunu, varsa gövdeyi dönüyoruz.
                if (envelope!.Body != null)
                {
                    context.Result = new ObjectResult(envelope.Body) { StatusCode = envelope.StatusCode };
                }
                else
                {
                    context.Result = new StatusCodeResult(envelope.StatusCode); //body'siz bir sonuç varsa sadece status code dönüyorsun.
                }
                return;
            }

            var executedContext = await next(); //  "ARTIK  controller action'ını çalıştır." DİYORSUN.  executedContext->  Controller çalıştıktan sonra oluşan sonuçtur.

            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                // İşlem hata ile sonuçlandı. Kilidi kaldırıyoruz ki tekrar denenebilsin.
                await db.KeyDeleteAsync(redisKey);
                _logger.LogWarning("Idempotent işlem hata ile sonuçlandı, kilit kaldırıldı. Key: {Key}", redisKey);
                return;
            }

            // --- GÜNCELLENEN KISIM: Başarılı Sonuçları Cache'e Yazma ---
            
            if (executedContext.Result is ObjectResult objResult)
            {
                // 1. Gövdeli dönüşler (Örn: Ok(data), BadRequest(hata))
                var statusCode = objResult.StatusCode ?? StatusCodes.Status200OK; // ?? -> Sol taraf null değilse onu kullan, null ise sağ tarafı kullan.
                var envelope = new IdempotentResponseEnvelope(statusCode, objResult.Value);
                await db.StringSetAsync(redisKey, JsonSerializer.Serialize(envelope), CompletedTtl);
            }
            else if (executedContext.Result is IStatusCodeActionResult statusCodeResult)
            {
                // 2. Gövdesiz dönüşler (Örn: Ok(), NoContent(), Unauthorized())
                var statusCode = statusCodeResult.StatusCode ?? StatusCodes.Status200OK;
                var envelope = new IdempotentResponseEnvelope(statusCode, null);
                await db.StringSetAsync(redisKey, JsonSerializer.Serialize(envelope), CompletedTtl);
            }
            else
            {
                // Beklenmeyen bir sonuç tipi (FileResult, ViewResult vb.) -> güvenli tarafta kalıp kilidi kaldırıyoruz.
                await db.KeyDeleteAsync(redisKey);
                _logger.LogWarning("Idempotent işlem beklenmeyen bir ActionResult tipi döndü, kilit kaldırıldı. Key: {Key}", redisKey);
                //"Bu sonucu güvenli şekilde Redis'e kaydedemiyorum." diyorsun . fileresult view result gibi Bunları Redis'e serialize edip tekrar döndürmek güvenli olmayabilir.
            }
        }
    }
}