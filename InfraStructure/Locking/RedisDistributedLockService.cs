// Infrastructure/Locking/RedisDistributedLockService.cs

using StackExchange.Redis;
using TrendyolMiniApi.Markers;

public class RedisDistributedLockService : IDistributedLockService , ISingletonService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        IEnumerable<string> resourceKeys,
        string ownerId,
        Func<Task<TResult>> action,
        DistributedLockOptions? options = null)
    {
        options ??= new DistributedLockOptions(); //Eğer options null ise, yeni bir DistributedLockOptions oluştur ve options'a ata.
        var db = _redis.GetDatabase();

        // Deadlock önlemi: kaynakları her zaman aynı sırada kilitle (Ali/Veli senaryosu)
        var sortedKeys = resourceKeys.Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList(); //distinc-> tekrar etmesin demek.sadece birer tane olsun

        for (int attempt = options.MaxRetries; attempt > 0; attempt--)
        {
            var acquiredLocks = new List<string>();
            try
            {
                foreach (var key in sortedKeys)
                {
                    var lockKey = $"lock:{key}";
                    bool isLocked = await db.LockTakeAsync(lockKey, ownerId, options.LockTimeout);

                    if (!isLocked)
                        throw new DistributedLockAcquisitionException(key);

                    acquiredLocks.Add(lockKey);
                }
                return await action();
            }
            catch (Exception ex) when (options.ShouldRetry(ex)) //when -> sadece bu şartı sağlıyorsa hatayı yakala demek
            //bu mekanizmayı kullanacağın serviste "ShouldRetry = ex => ex is DbUpdateConcurrencyException; gibi girersin. bu fonksiyon DbUpdateConcurrencyException gelirse true,başka hata gelirse false döndürüyordu
            {
                if (attempt == 1)
                    throw new InvalidOperationException(
                        "Sistemde anlık bir yoğunluk var. Lütfen tekrar deneyin.", ex);

                if (options.OnRetry != null)
                    await options.OnRetry();
            }
            finally
            {
                foreach (var lockKey in acquiredLocks)
                    await db.LockReleaseAsync(lockKey, ownerId); //ownerId , Kilidi kimin aldığını gösteren benzersiz değerdir.
            }
        }

        throw new InvalidOperationException("Beklenmeyen bir hata oluştu.");
    }
}