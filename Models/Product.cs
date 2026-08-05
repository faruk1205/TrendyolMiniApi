using System.ComponentModel.DataAnnotations;
using TrendyolMiniApi.Entities;

namespace TrendyolMiniApi.Models
{
    public class Product : SoftDeleteBaseEntity 
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } // Parasal işlemlerde float/double yerine "decimal" kullanılır!
        
        [ConcurrencyCheck]  //Bu etiket, stoğu güncellerken kilit mekanizmasını devreye sokar.Amaç  Optimistic concurrency 'yi önlemek. Mantığı ise kaydetme sırasında satırın beklediği sürümde olup olmadığını kontrol eder.
        public int Stock { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        
      
        // Foreign Keys
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int SellerId { get; set; }
        public User? Seller { get; set; }

        
        // İlişkiler
        public List<Favorite> Favorites { get; set; } = new();
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}

// [ConcurrencyCheck]  etiketi :
//diyelimki 5 farklı kişi aynı anda son 1 adet kalmış ürünü almak için istek attı
/*Normalde (Etiket yokken) 5 istek de şunu yapardı:
UPDATE Products SET Stock = 0 WHERE Id = 56   (5'i de çalışır, 5'i de başarılı dönerdi, stok eksiye düşerdi.)*/
/*Etiket varken EF Core şunu yapar:
 UPDATE Products SET Stock = 0 WHERE Id = 56 AND Stock = 1
 İşte kilit nokta burasıdır! O 5 istekten en hızlı olan 1 tanesi veritabanına ilk ulaştığında bu sorguyu çalıştırır,
  stok 0 olur. Arkasından gelen diğer 4 istek, saniyenin binde biri hızla aynı sorguyu çalıştırdığında AND Stock = 1 şartı artık 
  sağlanmadığı için EF Core hata fırlatır (Çünkü etkilenen satır sayısı 0 olur).*/


 