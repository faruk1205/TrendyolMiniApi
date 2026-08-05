// Infrastructure/Locking/DistributedLockAcquisitionException.cs
// Kilit alınamadığında fırlatılır (başkası zaten o kaynağı kilitlemiş)
public class DistributedLockAcquisitionException : InvalidOperationException
{
    public string ResourceKey { get; }
    public DistributedLockAcquisitionException(string resourceKey)
        : base($"'{resourceKey}' kaynağı için kilit alınamadı.")
    {
        ResourceKey = resourceKey;
    }
}

/*GlobalError Handling varken buna gerek var mıydı ? EVET ÇÜNKÜ:
 
DistributedLockAcquisitionException'ın işi InvalidOperationException fırlatmaktan farklı,
çünkü yapılandırılmış veri taşıyor (ResourceKey). Bunu InvalidOperationException("... meşgul ...")
gibi düz bir string ile fırlatsaydın, CheckoutAsync içindeki catch bloğunda hangi ürünün kilitli olduğunu 
hangi ürün adının mesaja gireceğini anlamak için string parse etmen gerekirdi:
 
    // custom exception olmasaydı böyle çirkin bir şey yapman gerekirdi
    catch (InvalidOperationException ex) when (ex.Message.Contains("meşgul"))
    {
    // hangi ürün? mesajdan regex mi çekeceksin şimdi ürün ID'sini?
    }
 
ResourceKey property'si sayesinde hatayı tip güvenli şekilde yakalayıp anlamlı bir kullanıcı mesajı üretebiliyordun:

    catch (DistributedLockAcquisitionException ex)
    {
    var productName = cartItems.FirstOrDefault(c => $"product:{c.ProductId}" == ex.ResourceKey)?.Product?.Name;
    throw new InvalidOperationException($"'{productName}' ürünü şu an başka bir müşteri tarafından satın alınıyor.");
    }
    
Bu, custom exception yazmanın klasik ve doğru gerekçesi: exception'ın taşıdığı ek veriye programatik erişim gerekiyor.
Ama işte tuzak: GlobalExceptionHandler'ın haberi yok
Senin GlobalExceptionHandler'ındaki switch şunu biliyor:

    var statusCode = exception switch
    {
        KeyNotFoundException => 404,
        InvalidOperationException => 400,
        UnauthorizedAccessException => 403,
        _ => 500   // <-- DistributedLockAcquisitionException buraya düşer!
    };

DistributedLockAcquisitionException, Exception'dan direkt türediği için (ne InvalidOperationException'dan ne başka tanınan tipten)
eğer birileri onu bir yerde catch edip dönüştürmeyi unutursa, bu switch'e düşer ve kullanıcıya "kilit meşgul, tekrar dene" yerine 
500 Internal Server Error + "beklenmeyen hata oluştu" döner. Bu yanlış — kaynak meşgulü, sunucu hatası değil, geçici bir çakışma.
Senin mevcut CheckoutAsync implementasyonunda bunu zaten catch edip InvalidOperationException'a çeviriyordun, o yüzden şu an sorun yok.
Ama bu davranış tamamen çağıranın disiplinine bağlı — yarın RedisLockService'i başka bir yerde kullanan biri bu catch'i unutursa, 
kullanıcı yanlış status code ve yanlış mesaj görür.
İki düzeltme seçeneği:
Seçenek A — DistributedLockAcquisitionException'ı InvalidOperationException'dan türet (en basit, en güvenli varsayılan):

        public class DistributedLockAcquisitionException : InvalidOperationException{ ...}
    
Böylece kimse özel olarak yakalamasa bile switch otomatik 400'e düşer, doğru davranış "varsayılan" olur. ResourceKey property'si
 hâlâ orada — isteyen özel olarak yakalayıp özelleştirebilir, istemeyen genel InvalidOperationException davranışını alır.
 
Seçenek B — switch'e ayrı case ekle (daha doğru HTTP semantiği):
Kaynak meşgulü aslında 400 Bad Request değil, 409 Conflict semantiğine daha yakın (istemcinin isteği hatalı değil, sunucudaki geçici bir durumla çakışıyor)

    var statusCode = exception switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        DistributedLockAcquisitionException => StatusCodes.Status409Conflict,  // yeni
        InvalidOperationException => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
    
Not: switch pattern matching'de sıralama önemli — DistributedLockAcquisitionException case'i InvalidOperationException'dan 
önce gelmeli (eğer Seçenek A'yı da uygularsan), yoksa daha genel olan üstteki case önce eşleşir ve alttaki hiç çalışmaz.

İKİ YÖNTEMİDE BİRLİKTE UYGULARSAK 
InvalidOperationException'dan türet (Seçenek A) — bu sana "unutulursa bile makul bir varsayılan" garantisi verir — ve 
switch'e 409 case'i ekle (Seçenek B) — bu da doğru semantiği verir. İkisi çelişmiyor, birbirini tamamlıyor:
    */

