using TrendyolMiniApi.Entities;

namespace TrendyolMiniApi.Models
{
    public class Favorite : BaseEntity 
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
//Bir kullanıcı favorilerden bir ürünü sildiğinde, bunu veritabanında saklamanın bize (veri analizi dışında) pek bir faydası yoktur. Bu bir ara tablodur.
//Eğer Favorite modeline ISoftDeletable arayüzünü (interface) eklemediysen, yazdığımız Interceptor bu tabloyu görmezden gelir. _context.Favorites.Remove(favorite) komutu veritabanında gerçek bir DELETE komutu çalıştırır.