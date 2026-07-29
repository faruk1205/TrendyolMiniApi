using System.Security.Claims;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    // Middleware'ler uygulama başlarken Singleton olarak ayağa kalkar.
    // Bu yüzden Scoped olan 'CurrentUser' nesnesini constructor'da DEĞİL, 
    // her istekte baştan çalışan InvokeAsync metodunda parametre olarak alıyoruz.
    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser)
    {
        // Kullanıcı giriş yapmışsa (Token geçerliyse ve Auth middleware'den geçmişse)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int userId))
            {
                currentUser.Id = userId;
            }

            currentUser.Email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            currentUser.Role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // İsteği sonrakine ilet
        await _next(context);
    }
}       
/*`Identity`, doğrudan token içindeki claim'lere erişmek için kullanılan nesne değildir. Authentication middleware, gelen token'ı doğruladıktan sonra token'daki claim'lerden bir `ClaimsIdentity` oluşturur ve bunu `HttpContext.User` içerisine yerleştirir.
Bu nedenle `context.User.Identity?.IsAuthenticated` ifadesi, kullanıcının doğrulanmış (authenticate edilmiş) olup olmadığını kontrol etmek için kullanılır.
Token içindeki claim'lere ise `context.User.Claims` koleksiyonu veya `FindFirst()` gibi metotlar aracılığıyla erişilir.
Örneğin:

```csharp
var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
```

Özetle, `Identity` kullanıcının kimlik doğrulama durumunu temsil eder; token'dan gelen claim'ler ise `User` nesnesi üzerinden okunur.
*/