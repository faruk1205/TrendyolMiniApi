public class DistributedLockOptions
{
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(10);    //TimeSpan bir zaman süresi tipidir 5 saniye, 2 dakika... gibi ama bir tarih değildir
    public int MaxRetries { get; set; } = 3;

    // Hangi exception'lar "tekrar dene" sebebi sayılsın? Varsayılan: hiçbiri (caller belirtmeli)
    public Func<Exception, bool> ShouldRetry { get; set; } = _ => false;
    #region MyRegion
    //Func -> metodu değişken gibi saklamaya yarar.
    // <Exception, bool> bu metodun girdisi Exception çıktısı boolean demek
    //_ => false bu ksım bir lambda ifadesidir. "Gelen exception ne olursa olsun" false döndür (default)
    #endregion
    
    // Retry öncesi çalıştırılacak temizlik (örn: ChangeTracker.Clear())
    public Func<Task>? OnRetry { get; set; } //Func<Task> bu methodun girdisi yok çıktısı task yani asenkron bir method 
}

#region MyRegion

/*Bu sınıfın genel amacı: Distributed Lock kullanan kodun davranışını dışarıdan ayarlanabilir hale getirmek.
Yani metot içinde sabit değerler kullanmak yerine, bunları bir nesne üzerinden yönetmek.
AYAR SINIFI OLMASAYDI ?

    public async Task ExecuteAsync()
    {
        int maxRetries = 3;
        int timeout = 10;

        ...
    }
Burada bütün ayarlar kodun içine gömülmüş durumda. Başka yerde 5 retry istiyorsun. Başka yerde timeout 30 saniye olsun istiyorsun. Başka yerde sadece belirli hatalarda retry yapılsın istiyorsun.
Hepsi için metodu değiştirmek gerekir. ayar dosyasıyla beraber ARTIK ÇAĞIRAN KİŞİ İSTEDİĞİ AYARI VEREBİLİR.

FONKSİYONLAR NEDEN BÖYLE DEĞİŞKEN GİBİ VERİLMİŞ ???

servisimize (RedisDistributedLockService) şunu diyoruz:
"Sana şimdiden evet veya hayır demiyorum. Sana bir Kural Kitapçığı (Fonksiyon) veriyorum. İçeride bir hata (Exception) patlarsa,
bu kural kitapçığını aç, hatanın tipine bak ve bana tekrar deneyip denemeyeceğimi sen söyle."
KULLANIRKENDE:

    ShouldRetry = ex => ex is DbUpdateConcurrencyException
    // "Gelen hata 'DbUpdateConcurrencyException' ise true dön, değilse false dön."


*/

#endregion
