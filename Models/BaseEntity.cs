namespace TrendyolMiniApi.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
//abstract (soyut) kelimesi, C# dünyasında bir sınıfın "Sadece miras alınabilir,
//ama asla tek başına var olamaz" olduğunu belirten çok güçlü bir mühürdür.
//(Soyut olmasının sebebi, tek başına "BaseEntity" diye bir tablonun
//veritabanında oluşmasını engellemektir).