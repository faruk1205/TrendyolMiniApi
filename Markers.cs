namespace TrendyolMiniApi.Markers
{
    // Her HTTP isteğinde yeni bir tane üretilen (En çok kullandığımız)
    public interface IScopedService { }

    // Her çağrıldığında yepyeni bir tane üretilen
    public interface ITransientService { }

    // Uygulama ayağa kalktığında 1 tane üretilip hep o kullanılan
    public interface ISingletonService { }
}



/*Bu kavramlar .NET Core'un Dependency Injection (Bağımlılık Enjeksiyonu) sisteminin kalbidir. Sisteme kaydettiğin bir servisin **ne zaman üretileceğini** ve **ne kadar yaşayacağını** (yaşam döngüsünü) belirlerler.
  
  Bunu akılda en kalıcı şekilde öğrenmek için bir **Restoran Analojisi** üzerinden gidelim:
  
  ## 1. Transient (Her Defasında Yepyeni)
  
  * **Restoran Örneği:** Peçete. Restoranda her "Peçete alabilir miyim?" dediğinde garson sana **yepyeni** bir peçete getirir. Kimse başkasının peçetesini kullanmaz, peçeteyi iki kere istemezsin.
  * **Yazılımdaki Karşılığı:** Uygulama içinde bu servisi her `IUserService` diye çağırdığında (ister aynı HTTP isteği içinde, ister farklı isteklerde) bellekte `new UserService()` diyerek yepyeni bir nesne üretilir. İşlem bitince de çöpe atılır (Garbage Collector temizler).
  * **Ne Zaman Kullanılır?**
  * İçinde veri (state) tutmayan, sadece bir işlem yapıp bitiren hafif servislerde.
  * Şifre hashleme sınıfları, ufak hesaplama yapan matematik servisleri veya e-posta gönderici sınıflarda (E-postayı atar ve işi biter).
  
  
  
  ## 2. Scoped (İstek Başına Tekil) - En Çok Kullanılan
  
  * **Restoran Örneği:** Masana bakan Garson. Restorana girdin, bir masaya oturdun. Yemeğin sonuna kadar (sipariş verirken, hesabı isterken) seninle **aynı garson** ilgilenir. Ama yan masaya (farklı bir müşteriye) farklı bir garson bakar.
  * **Yazılımdaki Karşılığı:** Bir kullanıcı API'ne bir HTTP isteği (Request) attığı anda bu servis bir kez üretilir. O istek sonlanana kadar (Controller, Service, Repository arasında gezinirken) hep aynı nesne kullanılır. İstek bittiğinde nesne ölür. Başka bir kullanıcı istek attığında ona özel yeni bir nesne üretilir.
  * **Ne Zaman Kullanılır?**
  * Web projelerindeki servislerin **%90'ı** Scoped'dur.
  * **Veritabanı bağlantıları (DbContext):** Bir kullanıcı işlem yaparken veritabanı bağlantısı açılır, işlemler aynı bağlantı üzerinden yapılır ve cevap dönüldüğünde kapatılır.
  * Kullanıcıya özel iş kurallarını yürüten sınıflar (`OrderManager`, `ProductService`).
  
  
  
  ## 3. Singleton (Uygulama Başına Tekil)
  
  * **Restoran Örneği:** Restoranın Ana Kasası. Restoranda yüzlerce müşteri, onlarca garson olabilir ama ödemelerin toplandığı tek bir kasa vardır. Kasa sabah dükkan açıldığında kurulur, akşam kapanana kadar herkes aynı kasayı kullanır.
  * **Yazılımdaki Karşılığı:** Uygulama (API) ilk ayağa kalktığında bellekte sadece **bir kez** üretilir. Uygulama kapanana kadar, kaç milyon kullanıcı istek atarsa atsın herkes bellekteki bu **aynı** nesneyi kullanır.
  * **Ne Zaman Kullanılır?**
  * Verilerin herkes için aynı olduğu ve sık sık değişmediği durumlarda.
  * **Cache (Önbellek) mekanizmaları:** Redis bağlantısı veya bellekte tutulan kur kodları.
  * Ayar dosyalarını okuyan konfigürasyon sınıfları.
  * SignalR (`ChatHub` gibi) canlı bağlantı yöneticileri.
  
  
  
  ---
  
  ### Kısa Bir Özet (Kodlama Yaparken Karar Rehberi)
  
  1. **İçinde veritabanı işlemi (`DbContext`) var mı?** $\rightarrow$ Kesinlikle **Scoped** yap. (Çünkü `DbContext` doğası gereği Scoped'dur).
  2. **Uygulama boyunca herkesin ortak kullanacağı, sabit bir veri veya bağlantı mı?** $\rightarrow$ **Singleton** yap.
  3. **Çok basit, veri tutmayan, "gir-çık" yapan bir hesaplama metodu mu?** $\rightarrow$ **Transient** yap.*/  
 

/*peki biz uygulamamıza inject ederken hangi türden eklediğimizi nasıl anlıyoruz
    Uygulama ayağa kalkarken (Program.cs çalışırken) hangi servisin hangi yaşam döngüsüne sahip olacağını sen belirlersin. Bir önceki adımda kurduğumuz Marker Interface sistemi sayesinde bu çok basittir.
    
    // "Ben bir Scoped servisim" dedin.
    public class PaymentManager : IPaymentManager, IScopedService 
    { 
        // ...
    }
    
    // "Ben bir Singleton servisim" dedin.
    public class CacheManager : ICacheManager, ISingletonService 
    { 
        // ...
    }
    Uygulama başladığında Scrutor bu etiketleri okur ve .NET'in defterine (DI Container) not alır: "PaymentManager her istekte yeni verilecek (Scoped), CacheManager ise herkese aynı verilecek (Singleton)."*/

        /*evet ama bu etiket arayüzleri biz yeni yaptık normalde yoktu?? 
        Çok haklısın, harika bir detay yakaladın! Bu "Marker Interface" (Etiket Arayüzleri) .NET'in kendi içinde varsayılan olarak gelen bir özellik değildir. Bu, büyük projeleri yönetmek için yazılımcıların (bizim) sonradan kurduğu akıllıca bir **mimari taktiktir**.
  
  Peki biz bu etiketleri icat etmeden önce, "normalde" (saf .NET Core'da) bu iş nasıl yapılıyordu?
  
  İki geleneksel yöntemi var:
  
  ## 1. Yöntem: Manuel Kayıt (Saf .NET Yolu)
  
  Eğer Scrutor gibi otomatik tarayan bir kütüphane kullanmasaydın, yazdığın her bir servisi `Program.cs` (veya `ServiceCollectionExtensions` sınıfın) içine **tek tek, elinle** kaydetmek zorundaydın.
  
  Hangi yaşam döngüsünü kullanacağını işte tam bu kayıt sırasında .NET'e söylersin. .NET sana 3 farklı metot sunar:
  
  ```csharp
  // 1. Scoped olsun istiyorsan: AddScoped metodunu kullanırsın
  services.AddScoped<IUserService, UserService>();
  services.AddScoped<IProductManager, ProductManager>();
  
  // 2. Transient olsun istiyorsan: AddTransient metodunu kullanırsın
  services.AddTransient<IEmailSender, EmailSender>();
  
  // 3. Singleton olsun istiyorsan: AddSingleton metodunu kullanırsın
  services.AddSingleton<ICacheManager, CacheManager>();
  
  ```
  
  **Zorluğu:** Projende 100 tane servis varsa, buraya alt alta 100 satır kod yazman gerekir. Proje büyüdükçe burası tam bir çorbaya döner ve yeni bir servis yazdığında buraya eklemeyi unutursan uygulama patlar.
  
  ## 2. Yöntem: Senin İlk Yazdığın Scrutor Kodu (Toplu ama Sabit Kayıt)
  
  Senin bana en başta gönderdiğin koddaki Scrutor mantığında ise şöyle bir durum vardı:
  
  ```csharp
  builder.Services.Scan(scan => scan
      .FromAssemblyOf<Program>()
      .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
      .AsImplementedInterfaces()
      .WithScopedLifetime() // BÜTÜN SIR BURADA!
  );
  
  ```
  
  Senin ilk kodun .NET'e şunu diyordu:
  *"Adı 'Service' ile biten bütün sınıfları bul ve **hepsini istisnasız Scoped olarak** kaydet."*
  
  Bu kod otomatikti, evet. Ama sana bir esneklik sunmuyordu. Eğer adı `CacheService` olan ve **Singleton** olması gereken bir sınıf yazsaydın, bu kod onu da acımasızca `Scoped` yapacaktı. Çünkü kodda `.WithScopedLifetime()` ile hepsini tek bir kalıba sokuyordun.
  
  ---
  
  ### Özetle Biz Ne Yapmış Olduk?
  
  1. **Eski Hali (Saf .NET):** Her şeyi tek tek elinle yazarsın. Esnektir ama çok ameleliktir.
  2. **Senin İlk Kodun:** Otomatiktir ama esnek değildir. Sadece tek tip (Scoped) ve tek isim (Service) kabul eder.
  3. **Bizim Kurduğumuz Etiket (Marker) Sistemi:** Hem otomatiktir (Scrutor kendi bulur, elinle yazmazsın) hem de esnektir (sınıfa koyduğun etikete göre kimisi Scoped, kimisi Singleton olur).
  
  Yani .NET'in normalde `AddScoped`, `AddSingleton` diyerek elle yaptırdığı işi, biz etiketler sayesinde Scrutor'a otomatik yaptırmış olduk.*/