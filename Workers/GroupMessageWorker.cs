using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Dtos;
using TrendyolMiniApi.Enums;
using TrendyolMiniApi.Hubs;

namespace TrendyolMiniApi.Workers
{
    public class GroupMessageWorker : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IServiceProvider _serviceProvider; //_serviceProvider: Arka plan servisi Singleton (tekil) olduğu için, Scoped (istek başına) çalışan DbContext gibi servisleri güvenle oluşturmak için kullanılır.
        private readonly ILogger<GroupMessageWorker> _logger;

        private const string QueueKey = "group-chat-queue"; //okunacak ana kuyruk adı
        private const string DeadLetterKey = "group-chat-queue:dead-letter"; //üst üste hata veren mesajların atılacağı ölü mektup kuyruğu
        private const int MaxRetryCount = 3;

        public GroupMessageWorker(
            IConnectionMultiplexer redis,
            IHubContext<ChatHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<GroupMessageWorker> logger)
        {
            _redis = redis;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        //Bu metot, BackgroundService'in kalbidir ve uygulama kapanana kadar çalışır. 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) //döngü içinde  redis kuyruğundan pop eder ve tane tane işlenmesi için diğer fonksiyona gönderir
        {
            var db = _redis.GetDatabase();

            while (!stoppingToken.IsCancellationRequested) //Uygulama (sunucu) kapatılma isteği almadığı sürece bu döngü sonsuza kadar döner.
            {
                RedisValue queueItem; //RedisValue, StackExchange.Redis kütüphanesinde tanımlı bir struct (değer tipi) bir yapıdır. Redis'ten okunan veya Redis'e yazılan ham veriyi temsil eder.

                try
                {
                    queueItem = await db.ListLeftPopAsync(QueueKey); //Redis kuyruğunun sol (baş) tarafından bir mesajı okur ve kuyruktan siler.
                }
                catch (Exception ex)
                {
                    // Redis'e erişilemiyorsa döngüyü öldürmeden bekle ve tekrar dene
                    _logger.LogError(ex, "Redis'ten okuma başarısız oldu.");
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                if (!queueItem.HasValue) //Eğer kuyrukta mesaj yoksa, CPU'nun %100'e fırlamasını (boş yere sürekli dönmesini) engellemek için döngü 50 milisaniye uyutulur ve başa döner.
                {
                    await Task.Delay(50, stoppingToken);
                    continue;
                }

                // Sorun #2 çözümü: her mesaj kendi try/catch'inde işlenir,
                // biri patlarsa döngü durmaz, sıradaki mesaja geçilir.
                try
                {
                    await ProcessMessageAsync(queueItem.ToString(), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mesaj işlenirken beklenmeyen hata: {Payload}", queueItem.ToString());
                }
            }
        }

        private async Task ProcessMessageAsync(string payload, CancellationToken stoppingToken)
        {
            var dto = GroupMessageQueueDto.FromJson(payload);
            if (dto is null)
            {
                _logger.LogWarning("Bozuk JSON, atlanıyor: {Payload}", payload);
                return;
            }

            using var scope = _serviceProvider.CreateScope(); //singleton içerisinde scope dbContex nesnesi kullandığımız için bu lazım
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var message = await dbContext.GroupMessages
                .FirstOrDefaultAsync(m => m.Id == dto.MessageId, stoppingToken);

            if (message is null)
            {
                _logger.LogWarning("DB'de bulunamayan mesaj ID'si: {Id}", dto.MessageId);
                return;
            }

            if (message.Status == MessageStatus.Sent)
                return; // Zaten işlenmiş, tekrar gönderme (idempotency)

            try
            {
                await _hubContext.Clients.Group(dto.GroupId.ToString())
                    .SendAsync("ReceiveGroupMessage", message.Id, message.SenderId, message.Content, cancellationToken: stoppingToken);

                message.Status = MessageStatus.Sent;
                message.CreatedDate = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                message.RetryCount++;

                if (message.RetryCount >= MaxRetryCount)
                {
                    message.Status = MessageStatus.Failed;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // Sorun #2 çözümü: pes edilen mesaj dead-letter listesine düşer,
                    // sonradan manuel/otomatik incelenebilir.
                    var db = _redis.GetDatabase();
                    await db.ListRightPushAsync(DeadLetterKey, payload);

                    _logger.LogError(ex, "Mesaj {Id} {Retry} denemeden sonra dead-letter'a düştü.",
                        message.Id, MaxRetryCount);
                }
                else
                {
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // Tekrar denemek için kuyruğa geri koy
                    var db = _redis.GetDatabase();
                    await db.ListRightPushAsync(QueueKey, payload);
                }
            }
        }
    }
}
/*GlobalExceptionHandler sadece HTTP request pipeline'ında çalışır

IExceptionHandler, ASP.NET Core'un middleware zincirine bağlıdır — yani sadece bir HTTP isteği işlenirken (Controller içinde, Middleware içinde)
 fırlatılan exception'ları yakalar. Mantığı şöyle: istek gelir → middleware'ler sırayla çalışır → biri patlarsa pipeline bunu TryHandleAsync'e yönlendirir → siz ProblemDetails dönersiniz.

GroupMessageWorker ise bir BackgroundService — uygulama başladığı anda kendi sonsuz while döngüsünde, hiçbir HTTP isteğine bağlı olmadan arka planda çalışır. Orada bir exception fırlarsa, 
ortada yakalayacak bir "istek" yok, dolayısıyla GlobalExceptionHandler bu hatayı asla göremez — pipeline'ın dışında bir olay bu.

Eğer worker'da try/catch + logger olmasaydı ne olurdu:

Exception, ExecuteAsync metodunun dışına taşardı.
.NET'in host'u bunu yakalar ama genelde sonucu şu olur: BackgroundService tamamen durur (bazı konfigürasyonlarda tüm uygulama crash bile edebilir — IHostApplicationLifetime ayarına bağlı).
Hiçbir yerde loglanmaz, hiçbir ProblemDetails dönmez — çünkü dönecek bir HTTP response yok, dönecek kimse yok.
Sonuç: grup mesajlaşma sistemi sessizce ölür, siz haftalar sonra "neden kimse mesaj alamıyor" diye fark edersiniz.*/