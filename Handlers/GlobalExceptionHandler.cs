using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TrendyolMiniApi.Middlewares
{
    // Sınıfımız artık IExceptionHandler arayüzünden miras alıyor
    //Burada diyorsun ki Ben ASP.NET Core'un Exception Handler'ıyım.Yani uygulama hata aldığında beni çağır.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        //logger nesnesinin görevi Console'a yazmak,Serilog'a göndermek,Dosyaya yazmak,Elasticsearch'e göndermekgibi işlemlerdir

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        // Hata fırlatıldığında .NET otomatik olarak bu metodu tetikleyecek
        //ValueTask, Task'ın daha performanslı bir alternatifidir.sonunda bir bool döndürür.Task çoğu zaman bellekte yeni bir nesne oluşturur. ValueTask, bazı durumlarda yeni nesne oluşturmadan çalışabilir.
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // 1. Hatayı Logla
            _logger.LogError(exception, "Sistemde bir hata oluştu: {Message}", exception.Message);

            // 2. HATA TİPİNE GÖRE STATUS KODU BELİRLE (İşte Sihir Burada)
            var statusCode = exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,           // Ürün bulunamadı
                InvalidOperationException => StatusCodes.Status400BadRequest,    // Zaten favorilerde ekli
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,   // Yetkisiz silme girişimi
                _ => StatusCodes.Status500InternalServerError                    // Öngörülemeyen diğer sistem hataları
            };

            // 3. Müşteriye Dönecek Formatı Hazırla
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == 500 ? "Sunucu Hatası" : "İşlem Başarısız",
                // 500 hatasıysa teknik detayı gizle, değilse bizim yazdığımız kendi hata mesajımızı (örn: "Ürün bulunamadı") göster
                Detail = statusCode == 500 ? "İşleminiz sırasında beklenmeyen bir hata oluştu." : exception.Message 
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; 
        }
            // return true: ben bu hatayı işledim  başka işlem yapma anlamına gelir.
            // false dönseydi bu handler işlenmedi başka handler var mı ? diye devam ederdi.
    }
}

/*Örneğin kullanıcı Postman'de şunu görür:
{
    "status":500,
    "title":"Sunucu Hatası",
    "detail":"İşleminiz sırasında beklenmeyen bir hata oluştu."
}
Ama log'da çok daha fazla bilgi vardır.
[16:42:15 ERR]

Sistemde kritik bir hata oluştu.

Message:
Object reference not set to an instance of an object.

StackTrace:
ProductService.GetById()
ProductController.Get()
... GİBİ */ 
