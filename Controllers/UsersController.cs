using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyolMiniApi.Data;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Sadece giriş yapmış olanlar (Rol fark etmez) bu işlemi yapabilir
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            // 1. JWT'den kullanıcının ID'sini al
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized("Kullanıcı kimliği bulunamadı.");

            int userId = int.Parse(userIdString);

            // 2. Kullanıcıyı bul
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // 3. Silmeyi Dene
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Hesabınız ve ona bağlı olan (izin verilen) tüm verileriniz başarıyla silindi." });
            }
            catch (DbUpdateException)
            {
                // EĞER RESTRICT KURALINA TAKILIRSA (Örn: Mesaj göndermişse veya Satıcıysa ve ürünü satılmışsa)
                return BadRequest("Hesabınız silinemiyor. Geçmişe dönük silinemez kayıtlarınız (Sipariş faturaları veya mesaj geçmişi) bulunmaktadır.");
            }
            /*Global hata yakalayıcı "beklenmeyen" kazalar içindir. Ancak bizim az önce yazdığımız silme işlemindeki kaza "beklenen ve planlanmış" bir kazaydı.
            Biz Restrict kuralını bilerek koyduk ve DbUpdateException hatasının fırlatılacağını biliyorduk. Burada try-catch kullanmamızın sebebi sunucuyu kurtarmak değil,
            İş Mantığını (Business Logic) yönlendirmekti Eğer bu hatayı Global Handler'a bıraksaydık, Global Handler bunun bir "Yabancı Anahtar (Foreign Key) İhlali"
            olduğunu anlayıp standart bir "500 Sunucu Hatası" dönebilirdi. Oysa biz lokal bir try-catch ile o hatayı pusuya düşürdük ve kullanıcıya özel, kibar bir
            400 Bad Request mesajı verdik: "Bu ürünü silemezsiniz çünkü faturası var!"*/
            
            /*Twitter'da: Amacımız her şeyi yok etmekti. SQL engelini aşmak için verileri manuel sildik. ( unutulma hakkı )
            Trendyol'da: Amacımız veriyi (faturaları) korumaktı. Manuel silmedik, bilerek SQL'in tokadını yedik ve işlemi iptal ettik.*/
        }
    }
}