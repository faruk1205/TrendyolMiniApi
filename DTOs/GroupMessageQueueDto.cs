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