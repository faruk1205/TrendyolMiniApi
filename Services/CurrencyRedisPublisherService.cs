using StackExchange.Redis;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services
{
    public interface ICurrencyRedisPublisherService
    {
        Task PublishLatestRateToRedisAsync();
    }

    public class CurrencyRedisPublisherService : ICurrencyRedisPublisherService, IScopedService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConnectionMultiplexer _redis;

        // DI havuzundaki mevcut _dbContext ve senin altyapıda kurduğun _redis otomatik gelecek
        public CurrencyRedisPublisherService(ApplicationDbContext dbContext, IConnectionMultiplexer redis)
        {
            _dbContext = dbContext;
            _redis = redis;
        }

        public async Task PublishLatestRateToRedisAsync()
        {
            var latestRate = _dbContext.ExchangeRates.FirstOrDefault(x => x.CurrencyCode == "USD");
            if (latestRate == null) return;

            try
            {
                var db = _redis.GetDatabase();  //Burada Redis'in veri saklama işlemlerinde kullanacağımız database nesnesini alıyoruz. Artık db üzerinden redise veri yazabilir/okuyabiliriz.
                var pubsub = _redis.GetSubscriber();//Burada Redis'in Publish/Subscribe (Pub/Sub) mekanizmasına erişiyoruz. db → Veri saklamak/okumak , pubsub → Diğer uygulamalara mesaj göndermek

                await db.StringSetAsync("usd-rate:latest", latestRate.Rate.ToString()); //Burada Redis'e bir key-value kaydediliyor.
                await pubsub.PublishAsync(RedisChannel.Literal("usd-rate-channel"), latestRate.Rate.ToString()); // redisin pub/sub sistemini kullanıyoruz. bir kanal oluşturup mesaj gönderiyoruz gibi düşünebilirsin.
            }
            catch (Exception ex)
            {
                // Loglayın ama fırlatmayın — DB kaydı zaten başarılı, Redis ikincil kanal
                Console.WriteLine($"Redis'e yayın başarısız: {ex.Message}");
            }
        }
    }
}
//StringSetAsync -Z güncel kuru rediste sakla
//PublishAsync -> kur değişti bundadn haberdar olmak isteyen herkese haber ver demektir

/*Bir de önemli bir nokta: Pub/Sub mesajları geçmişe dönük saklamaz. Subscriber o anda kanala
 bağlı değilse mesajı kaçırır. Bu yüzden senin kodunda hem Redis'e kaydetmek hem Pub/Sub ile bildirmek mantıklı bir pattern.*/