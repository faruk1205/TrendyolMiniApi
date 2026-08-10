using PuppeteerSharp;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services.Pdf
{
    // Uygulama ayağa kalktığında 1 kez çalışır ve 1 tane Chrome açar.
    public class PuppeteerBrowserProvider : ISingletonService, IAsyncDisposable //IAsyncDisposable: ".NET, bu uygulama kapandığında veya çöktüğünde RAM'i temizlemeyi unutma" diyen bir sözleşmedir
    {
        private IBrowser? _browser; //_browser: İçeride açık tutacağımız Chrome tarayıcısının ta kendisidir.
        private readonly SemaphoreSlim _lock = new(1, 1); //SemaphoreSlim: Bu, kulübün kapısındaki güvenlik görevlisidir. (1, 1) demek; "İçeriye aynı anda sadece 1 kişi (Thread) girebilir, diğerleri kapıda bekleyecek" demektir.

        public async Task<IBrowser> GetBrowserAsync()
        {
            //// 1. KONTROL: Dış Kapı
            if (_browser == null)
            {
                //// GÜVENLİK KONTROLÜ: Aynı anda gelen 50 kişiden sadece 1'i içeri alınır, 49'u bekletilir.
                await _lock.WaitAsync();
                try
                {
                    //// 2. KONTROL: İç Kapı (İşte sihir burada!)
                    if (_browser == null)
                    {
                        // Chrome'u indir (yoksa) ve arka planda (Headless) görünmez olarak başlat!
                        await new BrowserFetcher().DownloadAsync();
                        _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                    }
                }
                finally
                {
                    // İŞLEM BİTTİ: Güvenlik görevlisi kapıyı açar ve sıradakileri içeri alır.
                    _lock.Release();
                }
            }
            //// Zaten açıksa veya az önce açıldıysa, mevcut Chrome'u ver.
            return _browser;
        }

        public async ValueTask DisposeAsync()  //Eğer API sunucumuzu durdurursak, arka planda çalışan görünmez Chrome açık kalıp "Zombi Process" olarak bilgisayarının RAM'ini tüketmeye devam eder. Bu metot, uygulama kapanırken "Chrome'u da kapatmayı unutma" diyerek fişi temiz bir şekilde çeker.
        {
            if (_browser != null) await _browser.CloseAsync();
        }
    }
}
//Arka planda (Headless - yani ekranda hiçbir pencere açılmadan) görünmez bir Chrome sekmesi başlatır.
//Bizim Razor motoruyla oluşturduğumuz HTML metnini bu görünmez sekmeye yükler
//Sanki görünmez bir kullanıcı klavyeden Ctrl + P yapmış gibi Chrome'a "Bunu kenar boşlukları ayarlanmış bir A4 kağıdı formatında PDF'e çevir" emrini verir.
//Oluşan PDF dosyasını bize dijital (Byte) olarak teslim eder ve sekmeyi kapatır.