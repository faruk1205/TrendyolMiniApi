using System.Text.Json;

namespace TrendyolMiniApi.Dtos
{
    // Redis kuyruğuna artık içerik değil, sadece "hangi mesajı işle" bilgisi gidiyor.
    // Mesajın kendisi zaten Hub tarafından DB'ye Pending durumuyla yazılmış oluyor.
    public class GroupMessageQueueDto
    {
        public int MessageId { get; set; }
        public int GroupId { get; set; }

        public string ToJson() => JsonSerializer.Serialize(this); //Bu metot, C# nesnesini JSON string'e çeviriyor. buradaki this, o anda üzerinde çalıştığımız GroupMessageQueueDto nesnesini temsil eder.

        
        //Bu metodun görevi ToJson() metodunun tersini yapmak.
        public static GroupMessageQueueDto? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<GroupMessageQueueDto>(json);
            }
            catch (JsonException)
            {
                return null; // Bozuk JSON -> null döner, worker bunu loglayıp atlar
            }
        }
    }
}
/*DTO içine serialize/deserialize metodları yazdık çünkü:
1. "Bu nesne kendini nasıl taşıyacağını ve geri getireceğini bilir" (Kendi kendini yönetme),
2. Bozuk json gönderilmesine karşın null döndürerek hata yönetimini ele aldık..
catch (JsonException)
{
    return null; // Bozuk JSON -> null döner
} 
yani eğer dto'ya bu metodları yazmasaydık dışarıda sürekli :
try
{
    var dto = JsonSerializer.Deserialize<GroupMessageQueueDto>(payload);
    // işle...
}
catch (JsonException ex)
{
    _logger.LogWarning(ex, "Bozuk mesaj");
    continue;
} 
kontrolü yapmamız gerekirdi

ÖZETLE :
Evet, teknik olarak metod yazmasanız da olur. Hatta çoğu geliştirici DTO'lara metod eklemeyi sevmez ("anemic domain" diye eleştirir).
Ama bu özel durumda metod yazılmasının sebebi:
Hata yönetimini merkezileştirmek (try-catch'i tek bir yere almak)
Kod tekrarını önlemek (DRY)
Worker'ın bozuk mesaj yüzünden çökmesini engellemek (Dayanıklılık) */