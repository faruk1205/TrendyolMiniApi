using StackExchange.Redis;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services
{
    public class RateLimiterService : ISingletonService
    {
        private readonly IConnectionMultiplexer _redis;

        public RateLimiterService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        /// <summary>
        /// Kullanıcı başına basit sabit-pencereli rate limit.
        /// Örn: userId=5, action="group-msg", limit=10, window=1sn
        /// -> Bir saniyede en fazla 10 grup mesajı gönderebilir.
        /// </summary>
        public async Task<bool> IsAllowedAsync(int userId, string action, int limit, TimeSpan window)
        {
            var db = _redis.GetDatabase();
            var key = $"ratelimit:{action}:{userId}";

            var count = await db.StringIncrementAsync(key); //key daha önce yoksa oluşturulur ve değeri 1 yapılır zaten varsa 1 arttırılır
            if (count == 1)
            {
                // Pencerenin ilk isteği -> TTL'i burada kuruyoruz
                await db.KeyExpireAsync(key, window);//verilen süre sonrasında key silinir yani count sıfırlanmış olur
            }

            return count <= limit;
        }
    }
}
//Ayrı bir sınıfta tanımlanmaısnın sebebi farklı yerlerde de kullanılabilecek bir servis olması

/*rate limiting için redise ne gerek vardı peki -> çünkü eğer sayacı sunucunun belleğinde tutsaydık ve  load balancer ile birden fazla sunucuda çalıştırsaydık
bir kullanıcı sunucu A'ya 3 istek, sunucu B'ye 3 istek atarsa toplamda limiti aşmış olur ama hiçbir instance bunu tek başına göremez*/