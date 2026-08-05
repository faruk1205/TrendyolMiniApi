using Microsoft.EntityFrameworkCore;
using System.Linq;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Extensions
{
    public static class QueryableExtensions
    {
        // 'this IQueryable<T> query' sihirli kelimedir. 
        // "IQueryable üzerinden çağrıldığında bu metodu oraya yapıştır" demektir.
        public static IQueryable<T> IncludeSoftDeleted<T>(this IQueryable<T> query) where T : SoftDeleteBaseEntity
        {
            // EF Core 10'un isimli filtre kapatma özelliğini sorguya ekleyip geri yolluyoruz
            return query.IgnoreQueryFilters(["SoftDeleteFilter"]);
        }
    }
}
/*Çünkü **Hard Delete, sorguyla (query) ilgili değil; veriyi kaydetme (SaveChanges) süreciyle ilgilidir.** `QueryableExtensions`
sadece veriyi **nasıl okuyacağını** değiştirir. Örneğin `IncludeSoftDeleted()` metodu, normalde gizlenen (`IsDeleted = true`) kayıtları da 
sorguya dahil etmek için `IgnoreQueryFilters()` kullanır. Buna karşılık Hard Delete ise bir sorgu işlemi değildir; entity'nin silinme durumunu
değiştirir ve `SaveChanges()` sırasında EF Core'un `DELETE` mi yoksa `UPDATE IsDeleted = true` mı çalıştıracağına karar verilmesini gerektirir.
Bu kararın verildiği yer de `ChangeTracker` ve `SaveChangesAsync` olduğu için Hard Delete mantığının **ApplicationDbContext** içinde bulunması 
doğru tasarımdır. Kısacası, **`QueryableExtensions` okuma (read) davranışını özelleştirir, `ApplicationDbContext` ise yazma (write) davranışını 
yönetir.** Bu yüzden Hard Delete'in `ApplicationDbContext`'te olması mimari olarak daha doğru ve sorumlulukların ayrılması (Separation of Concerns) ilkesine uygundur.*/
