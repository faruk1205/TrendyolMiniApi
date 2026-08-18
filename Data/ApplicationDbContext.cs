    using Microsoft.EntityFrameworkCore;
    using TrendyolMiniApi.Entities;
    using TrendyolMiniApi.Models;
    using System.Reflection;

    namespace TrendyolMiniApi.Data
    {
        public class ApplicationDbContext : DbContext
        {
            private readonly HashSet<object> _hardDeletedEntities = new(); // 1. Gerçekten silinecek nesnelerin adreslerini tutacağımız özel VIP liste, (Arama işlemi hızlı olsun diye List yerine HashSet kullanıyoruz)
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

            // SQL'de oluşacak tablolarımızın listesi
            public DbSet<User> Users { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<Product> Products { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<OrderItem> OrderItems { get; set; }
            public DbSet<Favorite> Favorites { get; set; }
            public DbSet<Message> Messages { get; set; }
            
            public DbSet<CartItem> CartItems { get; set; }
            
            public DbSet<ExchangeRate> ExchangeRates { get; set; }
            
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                
                
                /*"Bir kullanıcı silinirse, onun gönderdiği mesajları da sileyim. Aynı kullanıcı silinirse,
                 onun aldığı mesajları da sileyim." İki silme kuralı aynı tabloya çarpıştığında sistem kilitlenir.*/
                // 1.Mesaj GÖNDEREN İLİŞKİSİ
                modelBuilder.Entity<Message>()
                    .HasOne(m => m.Sender)
                    .WithMany() // User modeline list olarak eklemediğimiz için WithMany() içini boş bırakıyoruz
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict); // Kullanıcı silinirse mesajlarını OTOMATİK SİLME!

                // 2.Mesaj ALICI İLİŞKİSİ
                modelBuilder.Entity<Message>()
                    .HasOne(m => m.Receiver)
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            
                
                // Email adresi veritabanında benzersiz olmalı
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.Email)
                    .IsUnique();

                // Kullanıcı adı (Username) veritabanında benzersiz olmalı
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.Username)
                    .IsUnique();

                // ESKİ KURAL (SİLİNECEK):
                // modelBuilder.Entity<Favorite>().HasKey(f => new { f.UserId, f.ProductId })
                //YENİ KURAL (EKLENECEK): index ile unique olma sorununu çözdük
                modelBuilder.Entity<Favorite>()
                    .HasIndex(f => new { f.UserId, f.ProductId })
                    .IsUnique(); // Bir kullanıcı, aynı ürünü sadece 1 kez favorileyebilir!
                
                // Bir müşteri, aynı ürünü sepette sadece 1 satır olarak tutabilir (Unique Index)
                modelBuilder.Entity<CartItem>()
                    .HasIndex(c => new { c.UserId, c.ProductId })
                    .IsUnique();
                
                // 2. KURAL: Parasal değerlerin (decimal) SQL'de ne kadar yer kaplayacağını belirtiyoruz (Uyarıları gizler)
                modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");                
                modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");

                // 3. KURAL: Canlı Chat (Message) tablosu için kritik "Kaskad Silme" engeli!
                // Bir kullanıcıyı silersek, ona ait mesajlar otomatik SİLİNMESİN, yoksa SQL hata verir.
                modelBuilder.Entity<Message>()
                    .HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<Message>()
                    .HasOne(m => m.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                
                // Müşteri silindiğinde, ONA AİT favoriler otomatik silinsin (Cascade)
                modelBuilder.Entity<Favorite>()
                    .HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ürün silindiğinde, o ürünü favorileyen herkesin listesinden de silinsin (Cascade)
                modelBuilder.Entity<Favorite>()
                    .HasOne(f => f.Product)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                //DİKKAT:
                //  Ürün silinmeye çalışıldığında, eğer bir siparişte (faturada) geçiyorsa SİLMEYİ REDDET!
                //ürünün silinmesi hakkında kural tanımlıyoruz ama orderItem üzerinde işlem yapıyoruz neden mi ?  . Çünkü FK'nin sahibi OrderItem.
                modelBuilder.Entity<OrderItem>()
                    .HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems) // (Product modelinde OrderItems listesi olduğunu varsayıyoruz)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict); // "Bu Product'a bağlı OrderItem varsa Product silinemez."
                
                
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())//GetEntityTypes() EF Core modelindeki bütün entity tiplerini döndürür. ( Product,Order... gibi)
                {
                    #region MyRegion
// "Bu entity, SoftDeleteBaseEntity sınıfından türemiş mi?"
                    // typeof(SoftDeleteBaseEntity) -> Bu ifade sınıfın kendisini temsil eden Type nesnesini verir. Yani SoftDeleteBaseEntity hakkındaki çalışma zamanı bilgisini alıyoruz.
                    //entityType.ClrType -> her entity'nin gerçek .NET tipi örneğin Product in typeof(Product) anlamına gelir
                    //ClrType -> CLR (Common Language Runtime), .NET uygulamalarını çalıştıran çalışma zamanıdır. Yani C# kodun doğrudan işletim sistemi tarafından çalıştırılmaz.CLR;Bellek yönetimini yapar. Garbage Collector'ı çalıştırır. Exception'ları yönetir. Kodunu çalıştırır. ClrType ise  EF Core modelindeki her entity'nin gerçek C# tipini temsil eder.
                    //IsAssignableFrom-> Product , SoftDeleteBaseEntity'den geliyor mu?
                    

                    #endregion 
                    if (typeof(SoftDeleteBaseEntity).IsAssignableFrom(entityType.ClrType))
                    {
                        #region MyRegion
// ApplySoftDeleteFilter metodunu bul ve çalıştır
                        /* methodu normal çağırmadık çünkü normalde bu fonksiyonu "ApplySoftDeleteFilter<Product>(modelBuilder);" gibi çağırırdık.
                         Dikkat burdaki Product'ı kendimiz yazıyoruz ama foreach içinde entityType'ı sürekli değişiyor yani runtime'da generik tip ne olacağını öğreniyor.
                         İşte reflection sayesinde "Generic tipi kod yazarken bilmiyorsan, çalışma zamanında ben oluştururum." diyoruz.*/
                        //var method = typeof(ApplicationDbContext) Burada ApplicationDbContext tipini alıyor. Amaç bu sınıfın içindeki metodları incelemek.
                        //".GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)"  -> "ApplicationDbContext içinde ApplySoftDeleteFilter isimli metodu bul."
                        //nameof(ApplySoftDeleteFilter) Şuna eşittir "ApplySoftDeleteFilter". Ama string yazmak yerine nameof kullanmak daha güvenlidir. Metodun adını değiştirirsen derleyici bunu yakalar.
                        //BindingFlags.NonPublic -> metod private olduğu için bunu söylüyor.
                        //BindingFlags.Instanc -> metod statik değil nesneye ait olduğu için söylüyor. "|" işareti normalde or'dur ama burda NonPublic VE Instance özelliklerine sahip üyeleri ara. anlamında kullanılmış.
                        

                        #endregion
                        var method = typeof(ApplicationDbContext)
                            .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.MakeGenericMethod(entityType.ClrType);

                        method?.Invoke(this, new object[] { modelBuilder });
                    }
                }// modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted); elle bu şekilde yazıcağımıza reflection ile otomatikleştirdik tek tek elle yazmamıza gerek yok.
            }
            
            private void ApplySoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : SoftDeleteBaseEntity //<T> → Generic metot.Yani aynı metot birçok entity için kullanılabilir.
            {
                modelBuilder.Entity<T>().HasQueryFilter("SoftDeleteFilter", e => !e.IsDeleted);
                #region MyRegion
                //where T : SoftDeleteBaseEntity -> "Bu metoda gönderilecek tip mutlaka SoftDeleteBaseEntity sınıfından türemiş olmalı."
                //.HasQueryFilter -> "Bu entity için tüm sorgulara uygulanacak varsayılan filtreyi belirle."

                #endregion
            }
            
            //<TEntity> yerine <T> 'de yazabilirdik bu sadece generic olduğunu belirtmek için 
            public void HardRemove<TEntity>(TEntity entity) where TEntity : class  //TEntity sadece referans tip (class) olabilir.
            {
                _hardDeletedEntities.Add(entity); // Nesneyi özel listemize ekle
                Set<TEntity>().Remove(entity); // EF Core'a standart silme komutunu ver (Şimdilik durumu 'Deleted' yapacak), Set<TEntity>(), "Bana TEntity tipindeki DbSet'i getir." demektir. Yani: Set<User>() aslında şununla aynıdır: Users
            }

            #region QueribleDeğiştirmeÖrneği 

            /*
             Senin yazdığın HardRemove metodu veritabanında (veya RAM'de) bir değişiklik yapmak üzere tasarlanmıştır.
             Neden Parametre Alıyor? (TEntity entity): Çünkü silmek için elinde fiziksel olarak var olan, RAM'de yaşayan bir nesneye ihtiyacın var. "Neyi sileyim?" sorusunun cevabıdır.
             Neden Geriye Bir Şey Dönmüyor? (void): Çünkü bu metot bir soru sormuyor, bir eylem gerçekleştiriyor. "Bu nesneyi al ve silinmiş olarak işaretle." İşlem bittiğinde sana geri vereceği bir veri (liste, ürün vb.) yoktur
             Çalışma Anı: Metot çalıştığı an RAM'deki ChangeTracker (Değişiklik Takipçisi) güncellenir.

             */
            
            /*
            // TEntity sadece referans tip (class) olabilir ve SoftDeleteBaseEntity'den türemelidir.

            public IQueryable<TEntity> GetAllIncludingDeleted<TEntity>() where TEntity : SoftDeleteBaseEntity
            {
                // _context.Set<TEntity>() -> "Bana dinamik olarak TEntity hangi tabloyaysa (Örn: Products) onu ver" demek.
                return this.Set<TEntity>().IgnoreQueryFilters(["SoftDeleteFilter"]);
            }

            /*
             bu metot ise bir eylem yapmaz, veritabanına soru sormak için bir hazırlık yapar.
             Neden Parametre Almıyor? (): Çünkü elinde henüz bir nesne yok! Amacımız zaten veritabanına gidip nesneleri bulup getirmek. Metoda sadece <TEntity> diyerek
              "Hangi tabloya gideceğimizi" (Örn: Products) söylüyoruz, içine bir veri fırlatmıyoruz.
              Neden IQueryable<TEntity> Dönüyor?: Burası en can alıcı noktadır. IQueryable demek, RAM'de verilerin kendisi demek değildir.
               IQueryable, SQL Server'a gönderilecek olan SQL Sorgusunun Taslağıdır (Bir reçetedir).
                Ne İşe Yarar?: Bu metot çalıştığında veritabanına gidilmez! Sadece C# arka planda şu SQL metnini hazırlar: "SELECT * FROM Products (Silinmişleri gizleme kuralını da iptal ettim)."
             */

            #endregion
            
            
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) 
            {
                try
                {
                    // ChangeTracker (Değişiklik Takipçisi) içindeki  ISoftDeletable arayüzünün implrmente eden tüm varlıkları bul
                    foreach (var entry in ChangeTracker.Entries<SoftDeleteBaseEntity>())
                    {
                        if (entry.State == EntityState.Deleted) // Eğer komut "Silme" komutuysa...
                        {
                            if (_hardDeletedEntities.Contains(entry.Entity))
                            {
                                continue; // Eğer listedeyse, hiçbir koda dokunma!  // Bırak durumu 'Deleted' olarak kalsın ki SQL'den fiziksel olarak silinsin.
                            }

                            entry.State = EntityState.Modified; // 1. Komutu "Güncelleme" olarak değiştir
                            entry.Entity.IsDeleted = true; // 2. IsDeleted alanını true yap
                        }
                    }
                    return
                        base.SaveChangesAsync(
                            cancellationToken); //base, mevcut sınıfın üst (parent) sınıfını ifade eder. //"Üst sınıftaki (DbContext) SaveChangesAsync metodunu çağır ve dönen sonucu olduğu gibi geri döndür." YANİ return base.SaveChangesAsync(cancellationToken); satırı olmasaydı, yaptığın değişiklikler yalnızca bellekte kalır, veritabanına yazılmazdı.
                }
                finally
                {
                    _hardDeletedEntities.Clear();
                }
            }
            
        } 
    }
        
        
    
    //save methodunu override ediyoruz çünkü remove methodunun kendisi sadece bir etiketlemedir  "Ürün ID 5 silinecek diye işaretlendi. (Durumu: EntityState.Deleted)" yapar.
    //Yani Remove metodu aslında veriyi silmez, sadece hafızadaki varlığın durumunu (State) değiştirir. Bu yüzden onu ezmek (override) bize gerçek bir güç kazandırmaz.asıl aksiyon await _context.SaveChangesAsync() dediğinde başlar. işlemi, save yapar.
    
    /*Change Tracker : Entity Framework Core'un bellekte takip ettiği entity (nesne) değişikliklerini izleyen mekanizmadır.
     Böylece SaveChanges() çağrıldığında hangi kayıtların ekleneceğini, güncelleneceğini veya silineceğini otomatik olarak belirler.
     /EntityEntry nedir?  EF Core, veritabanından çektiği her nesneyi takip eder. Arka planda EF bunun için bir EntityEntry oluşturur.Değişiklikleri takip eden şey EntityEntry değil, Change Tracker'dır.
     EntityEntry ise Change Tracker'ın tek bir entity için tuttuğu kayıtdır.*/
    
/*CancellationToken, uzun süren bir işlemin iptal edilmesini sağlayan bir mekanizmadır. SaveChangesAsync metodunda ise veritabanına kayıt işlemi devam ederken, çağıran taraf isterse bu işlemi iptal edebilir.*/
/*= default → Token gönderilmezse varsayılan (CancellationToken.None) kullanılır ve iptal edilemez. Hatta kendi Cancellation tokenunu bile oluşturabilirsin:

var cts = new CancellationTokenSource();

var task = context.SaveChangesAsync(cts.Token);

// 2 saniye sonra iptal et
cts.Cancel(); */