using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TrendyolMiniApi.Models
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Müşteri"; // "Müşteri" veya "Satıcı"

        // İlişkiler (Listeleri "new()" ile başlatıyoruz ki CS8618 null hatası almayalım)
        public List<Product> Products { get; set; } = new(); 
        public List<Favorite> Favorites { get; set; } = new();
        public List<Order> Orders { get; set; } = new();

        [InverseProperty("Sender")]
        public List<Message> SentMessages { get; set; } = new();

        [InverseProperty("Receiver")]
        public List<Message> ReceivedMessages { get; set; } = new();
    }
}
/*Ancak EF Core'un "Kullanıcı silinince ürünlerini de acımadan sil" (Cascade Delete) kararı 
almasının asıl sebebi, alt tablolardaki Yabancı Anahtarların (Foreign Key) nullable (boş bırakılabilir) OLMAMASIDIR*/
    
/*SQL Server gibi bazı veritabanları bu durumu gördüğünde "Dur! Aynı favoriyi iki farklı yoldan silmeye çalışıyorsun,
 ben bunu yapamam!" diyerek sistemi çökertir. Şanslıyız ki, PostgreSQL bu konuda çok daha akıllıdır ve bunu genellikle sorunsuz çözer.*/
 
 /*Favorite girdiğinde en fazla müşterinin beğendiği ürün listesi silinir, dünyanın sonu değildir. Ama işin içine Product (Ürün) girdiğinde,
e-ticaretin en kutsal verisi olan "Faturalar ve Siparişler" (Orders & OrderItems) tehlikeye girer!
    İşte bu yüzden, büyük projelerde ModelBuilder kullanmak bir lüks değil, zorunluluktur.*/
    
    
