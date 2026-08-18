using TrendyolMiniApi.Entities;    // ExchangeRate için gerekli
using TrendyolMiniApi.Providers;   // IExchangeRateProvider için gerekli
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Markers;

// using TrendyolMiniApi.Data; 

namespace TrendyolMiniApi.Services
{
    public interface ICurrencySyncService
    {
        Task SyncUsdRateAsync();
    }

    public class CurrencySyncService : ICurrencySyncService, IScopedService
    {
        private readonly ApplicationDbContext _dbContext;
        
        private readonly IEnumerable<IExchangeRateProvider> _providers;

        public CurrencySyncService(ApplicationDbContext dbContext, IEnumerable<IExchangeRateProvider> providers)
        {
            _dbContext = dbContext;
            _providers = providers;
        }

        public async Task SyncUsdRateAsync()
        {
            // Hangi API'nin çalışacağını ve kuru tutacağımız değişkenleri en başta tanımlıyoruz
            decimal currentRate = 0;
            string successfulProviderName = string.Empty;

            // 1. Alet çantasından hem asil hem de yedek oyuncuyu buluyoruz
            var primaryProvider = _providers.FirstOrDefault(p => p.ProviderType == "RestJson");
            var backupProvider = _providers.FirstOrDefault(p => p.ProviderType == "RestXml"); 

            if (primaryProvider == null || backupProvider == null) 
                throw new Exception("Provider sistemleri eksik yüklendi!");

            try
            {
                // 2. ÖNCE A PLANINI DENE (REST JSON)
                currentRate = await primaryProvider.GetTryExchangeRateAsync("USD");
                successfulProviderName = primaryProvider.ProviderType; 
                Console.WriteLine("ANA PROVİDER ÇALIŞTI (REST-JSON)");
            }
            catch (Exception ex)
            {
                // 3. A PLANI ÇÖKERSE (Örn: API yanıt vermedi) SİSTEMİ PATLATMA, BURAYA GİR!
                Console.WriteLine($"Ana Provider (REST) çöktü: {ex.Message}. Yedek (XML) devreye giriyor...");

                // B PLANINI ÇALIŞTIR (REST XML)
                currentRate = await backupProvider.GetTryExchangeRateAsync("USD");
                successfulProviderName = backupProvider.ProviderType;
            }

            // 4. Veritabanı Kayıt İşlemi (Buraya geldiğimizde ya REST ya da XML kesinlikle çalışmış demektir)
            var existing = _dbContext.ExchangeRates.FirstOrDefault(x => x.CurrencyCode == "USD");

            if (existing == null)
            {
                _dbContext.ExchangeRates.Add(new ExchangeRate 
                { 
                    CurrencyCode = "USD", 
                    Rate = currentRate, 
                    LastUpdated = DateTime.UtcNow, 
                    ProviderName = successfulProviderName // Hangi provider kurtardıysa onun adını yazıyoruz
                });
            }
            else
            {
                existing.Rate = currentRate;
                existing.LastUpdated = DateTime.UtcNow;
                existing.ProviderName = successfulProviderName;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
/*Hangfire işçisi (CurrencySyncService) ise tamamen başka bir boyuttadır. Dışarıdan bir müşteri tetiklemez,
 HTTP isteği yoktur. O kendi kendine gece yarısı uyanan, arka planda çalışan bağımsız bir hayalettir.
 Hayaletlerin HTTP boru hattıyla işi olmadığı için, Global Exception Handler arka planda patlayan hataları duyamaz ve yakalayamaz.*/
 //Biz o try-catch bloğunu basit bir "hata yakalama" mekanizması olarak değil, bir İş Akışı Makası (Workflow Switch) olarak kullandık.