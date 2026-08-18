using Hangfire;
using TrendyolMiniApi.Services;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Jobs
{
    public class CurrencyJobManager : ITransientService
    {
        public void TriggerSyncAndPublish()
        {
            // 1. Kuru çekme görevini tek seferlik kuyruğa at
            var jobId = BackgroundJob.Enqueue<ICurrencySyncService>(x => x.SyncUsdRateAsync());
            
            // 2. O görev bittiği an Redis'e gönderme görevini tetikle
            BackgroundJob.ContinueJobWith<ICurrencyRedisPublisherService>(jobId, x => x.PublishLatestRateToRedisAsync());
        }
    }
}