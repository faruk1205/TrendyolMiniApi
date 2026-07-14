    using Microsoft.EntityFrameworkCore;
    using TrendyolMiniApi.Models;

    namespace TrendyolMiniApi.Data
    {
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

            // SQL'de oluşacak tablolarımızın listesi
            public DbSet<User> Users { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<Product> Products { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<OrderItem> OrderItems { get; set; }
            public DbSet<Favorite> Favorites { get; set; }
            public DbSet<Message> Messages { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                
                // Email adresi veritabanında benzersiz olmalı
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.Email)
                    .IsUnique();

                // Kullanıcı adı (Username) veritabanında benzersiz olmalı
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.Username)
                    .IsUnique();

                // 1. KURAL: Favorite tablosunun tek bir Id'si yok, iki sütunun birleşimi anahtar (Composite Key)
                modelBuilder.Entity<Favorite>()
                    .HasKey(f => new { f.UserId, f.ProductId });

                // 2. KURAL: Parasal değerlerin (decimal) SQL'de ne kadar yer kaplayacağını belirtiyoruz (Uyarıları gizler)
                modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");                modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");

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


                // DİKKAT: Ürün silinmeye çalışıldığında, eğer bir siparişte (faturada) geçiyorsa SİLMEYİ REDDET!
                modelBuilder.Entity<OrderItem>()
                    .HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems) // (Product modelinde OrderItems listesi olduğunu varsayıyoruz)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict); // İŞTE SİGORTA BURASI!
            }
        }
    }