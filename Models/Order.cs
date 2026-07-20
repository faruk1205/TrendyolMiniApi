namespace TrendyolMiniApi.Models
{
    public class Order : BaseEntity
    {
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Hazırlanıyor"; // Hazırlanıyor, Kargoda, Teslim Edildi
        public decimal TotalAmount { get; set; }
        
        // Foreign Key
        public int UserId { get; set; }
        public User? User { get; set; }

        // İlişkiler
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
/*C# tarafında modelimizi yazarken public List<OrderItem> OrderItems { get; set; } yazdığımız için,
 insan doğal olarak veritabanı tablosuna bakınca da orada içi ürünlerle dolu bir "Liste" sütunu görmeyi bekliyor.*/
/*PostgreSQL, SQL Server, MySQL gibi ilişkisel veritabanları verileri Excel mantığıyla, düz satırlar ve sütunlar halinde tutar.
Bir SQL hücresinin içine koca bir listeyi, başka bir nesneyi veya bir diziyi (Array) gömemezsin (NoSQL veritabanları -örn: MongoDB- bunu yapabilir ama SQL yapamaz).*/
/*SQL bu liste tutamama sorununu Foreign Key (Yabancı Anahtar) mantığıyla çözer.Ekran görüntüsündeki Orders tablon bir Fatura Başlığıdır
.Sadece faturanın kime kesildiğini (UserId: 5), ne zaman kesildiğini ve genel toplamını (TotalAmount: 1599) tutar.
İçindeki detaylar ise (hangi üründen kaç tane alındığı) OrderItems isimli o köprü tablosunda tutulur. */    



