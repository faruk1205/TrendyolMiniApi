using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Hubs
{
    [Authorize] // GÜVENLİK DUVARI: Sadece giriş yapmış (JWT'si olan) kişiler telsize bağlanabilir.
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        // Veritabanına mesajları kaydetmek için context'imizi alıyoruz.
        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Frontend bu metodu tetikleyecek
        public async Task SendPrivateMessage(int receiverId, string content)
        {
            // 1. KİM GÖNDERİYOR?
            // Giriş yapan kişinin ID'sini token'dan otomatik yakalıyoruz. Kimse başkasının adına mesaj atamaz!
            /*Context.UserIdentifier claim değildir!! Bu, SignalR'ın kullanıcıyı tanımak için oluşturduğu benzersiz kullanıcı kimliğidir. SignalR bağlantısı kurulurken bunu belirler.
            yani arka planda Context.UserIdentifier = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value; gibi bir işlem çalışır  */
            
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString)) return; // Kimlik yoksa işlemi durdur
            
            int senderId = int.Parse(userIdString);

            // 2. VERİTABANINA KAYDET (Kalıcılık)
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 3. CANLI İLETİM (Sadece hedef kişiye)
            // "receiverId" ID'sine sahip kullanıcının telsiz frekansına bu mesajı canlı olarak yolla
            
            await Clients.User(receiverId.ToString()).SendAsync("ReceivePrivateMessage", senderId, content);
            
            // Clients denen şey hub'a bağlı tüm istemcilerdir.  Clients.User("8") sadece ID'si 8 olan kullanıcıya gönder. gibi.
            //.SendAsync(...) istemcide (frontend'de) bir metodu tetikler.
            /*"ReceivePrivateMessage" Bu aslında metodun adı değildir. Bir event (olay) adıdır. Backend, ReceivePrivateMessage gönderiyor. 
            frontend ReceivePrivateMessage olayını yakalıyor. İsimler aynı olmak zorundadır.!!!  */
        }
    }
}
//Hub istemciler arasındaki iletişimi yönetir.
/*Telefon Santrali

Ali  --------\
    \
Ayşe ---------> HUB ----------> Mehmet */

/*Normal web APİ'de   var userId = int.Parse( User.FindFirstValue(ClaimTypes.NameIdentifier) );
buradaki user  aslında HttpContext.User demektir.JWT doğrulandıktan sonra middleware tokenı okuyup claimleri buraya koyar. Bizde User.FindFirstValue(...) ile alırız */
/* SignalR ise bir HTTP isteği değildir. Normal Controller'da her istekte HttpContext oluşur. Ama SignalR'de ise tek bir bağlantı açılır ve sonra aynı bağlantı kullanılır 
Bu yüzden SignalR sana HubCallerContext verir. Yani Buradaki Context aslında HubCallerContext tipindedir. İçinde ConnectionId,User,UserIdentifier, Items, Features, Abort()... vardır.*/
