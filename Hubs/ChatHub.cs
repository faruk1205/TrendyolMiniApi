using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Dtos;
using TrendyolMiniApi.Enums;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConnectionMultiplexer _redis;
        private readonly RateLimiterService _rateLimiter;

        private const int MaxContentLength = 2000;
        private const int GroupMsgLimitPerSecond = 5;

        public ChatHub(
            ApplicationDbContext dbContext,
            IConnectionMultiplexer redis,
            RateLimiterService rateLimiter)
        {
            _dbContext = dbContext;
            _redis = redis;
            _rateLimiter = rateLimiter;
        }
        
        public async Task JoinGroup(int groupId)
        {
            var senderId = GetUserId();
            var isMember = await _dbContext.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == senderId);
            if (!isMember) throw new HubException("Bu gruba katılamazsınız.");
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        // ---------- 1-1 MESAJLAŞMA ----------
        public async Task<int> SendPrivateMessage(int receiverId, string content)
        {
            var senderId = GetUserId();

            content = ValidateAndTrimContent(content); // boş/aşırı uzun içerik burada elenir

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                IsRead = false
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            IReadOnlyList<string> targetUsers = new[] { receiverId.ToString(), senderId.ToString() };
            await Clients.Users(targetUsers).SendAsync("ReceivePrivateMessage", message.Id, senderId, content);

            return message.Id;
        }

        // ---------- GRUP MESAJLAŞMA ----------
        public async Task SendGroupMessage(int groupId, string content)
        {
            var senderId = GetUserId();

            content = ValidateAndTrimContent(content);

            // Sorun #4 çözümü: üye olmayan biri gruba mesaj atamaz
            var isMember = await _dbContext.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == senderId);

            if (!isMember)
                throw new HubException("Bu gruba mesaj gönderme yetkiniz yok.");

            // Sorun #6 çözümü: kullanıcı başına rate limit
            var allowed = await _rateLimiter.IsAllowedAsync(
                senderId, "group-msg", GroupMsgLimitPerSecond, TimeSpan.FromSeconds(1));

            if (!allowed)
                throw new HubException("Çok hızlı mesaj gönderiyorsunuz, lütfen yavaşlayın.");

            // Sorun #1 çözümü: mesajı ÖNCE DB'ye Pending olarak yaz.
            // Bu satırdan sonra Redis çökse veya worker patlasa bile mesaj kaybolmaz.
            var groupMessage = new GroupMessage
            {
                SenderId = senderId,
                GroupId = groupId,
                Content = content,
                Status = MessageStatus.Pending
            };

            _dbContext.GroupMessages.Add(groupMessage);
            await _dbContext.SaveChangesAsync();

            // Kuyruğa artık sadece ID gidiyor
            var dto = new GroupMessageQueueDto { MessageId = groupMessage.Id, GroupId = groupId };
            var db = _redis.GetDatabase();
            await db.ListRightPushAsync("group-chat-queue", dto.ToJson());

            // Sorun #5 çözümü: gönderene anında "kuyruğa alındı" onayı
            await Clients.Caller.SendAsync("GroupMessageQueued", groupMessage.Id);  //Clients.Caller, SignalR'da o anki bağlantıyı yapan istemciyi (yani mesajı gönderen kişiyi) temsil eder.
        }

        // ---------- Yardımcılar ----------
        private int GetUserId()
        {
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                throw new HubException("Kullanıcı kimliği doğrulanamadı.");

            return userId;
        }

        private static string ValidateAndTrimContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new HubException("Boş mesaj gönderilemez.");

            content = content.Trim();

            if (content.Length > MaxContentLength)
                throw new HubException($"Mesaj en fazla {MaxContentLength} karakter olabilir.");

            return content;
        }
    }
}


//**************************************************************************************************************************************************************
/*IHubContext<ChatHub>,bu GroupMessageWorker.cs 'de kullanılan ->
 size sadece "dışarıdan içeri" bir mikrofon verir — sunucunun, herhangi bir yerden (worker, controller, başka bir servis), istemcilere mesaj göndermesini sağlar
 await _hubContext.Clients.Group("1").SendAsync("ReceiveGroupMessage", ...);
 Bu satır, istemciye bir event fırlatır. Ama istemcinin size bir şey söylemesini (bir metod çağırmasını) sağlamaz — çünkü öyle bir mekanizma yok. IHubContext'in Clients.Group(...).SendAsync(...) 
 dışında istemciden gelen çağrıları dinleyecek hiçbir yapısı yok.
 Ayrıca Sunucu, istemci hiçbir şey sormadan ona mesaj gönderebilir. Bu, HTTP'de mümkün olmayan şey. GroupMessageWorker, hiçbir istemci hiçbir şey çağırmamışken,
  IHubContext ile canlı bağlı bir tarayıcıya ReceiveGroupMessage event'ini anında iter. HTTP'de böyle bir şey yok — sunucu asla inisiyatif alamaz, hep istemci sormak zorunda.
 
Ama hub sınıfımızda yani bu sınıfta mesela -> istemciden sunucuyadır yani
SendGroupMessage, JoinGroup gibi metodlar tam tersi yönde çalışıyor — istemci tarafında connection.invoke("SendGroupMessage", groupId, content) çağrıldığında,
SignalR bu isteği sizin ChatHub sınıfınızdaki o isimdeki metoda otomatik olarak yönlendiriyor. Bu eşleme (invoke("MetodAdı", ...) → Hub sınıfındaki MetodAdı),
SignalR'ın protokolünün temeli — sadece Hub'dan miras alan sınıflarda çalışır, başka hiçbir sınıfta çalışmaz.

Ayrıca Hub'a özel, başka yerde olmayan araçlar var
ChatHub içinde kullandığımız şu şeyler sadece bir Hub metodunun içinde mevcuttur, IHubContext üzerinden(mesela GroupMessageWorker.cs'ten) erişilemez:
Context.UserIdentifier — o an bağlanan kullanıcının kimliği
Context.ConnectionId — o spesifik bağlantının ID'si
Groups.AddToGroupAsync(...) — o an çağrıyı yapan bağlantıyı bir gruba ekleme
Clients.Caller — sadece o isteği atan istemciye cevap verme
Bunların hepsi "şu an bana kim, hangi bağlantıdan sesleniyor" bilgisine ihtiyaç duyar — bu bilgi sadece gerçek bir Hub çağrısı sırasında var olur,
IHubContext ile dışarıdan erişildiğinde yoktur (çünkü o an aktif bir "çağıran" yok, siz sadece dışarıdan bir yayın yapıyorsunuz).
//***************************************************************************************************************************************************************/

#region MyRegion




    /*[Authorize] // GÜVENLİK DUVARI: Sadece giriş yapmış (JWT'si olan) kişiler telsize bağlanabilir.
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
            yani arka planda Context.UserIdentifier = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value; gibi bir işlem çalışır  
            
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
            frontend ReceivePrivateMessage olayını yakalıyor. İsimler aynı olmak zorundadır.!!!  
        }
    }
}*/
#endregion
//Hub istemciler arasındaki iletişimi yönetir.
/*Telefon Santrali

Kullanıcı 1 ---> Sunucu/Hub ---> Kullanıcı 2

/*Normal web APİ'de   var userId = int.Parse( User.FindFirstValue(ClaimTypes.NameIdentifier) );
buradaki user  aslında HttpContext.User demektir.JWT doğrulandıktan sonra middleware tokenı okuyup claimleri buraya koyar. Bizde User.FindFirstValue(...) ile alırız */
/* SignalR ise bir HTTP isteği değildir. Normal Controller'da her istekte HttpContext oluşur. Ama SignalR'de ise tek bir bağlantı açılır ve sonra aynı bağlantı kullanılır 
Bu yüzden SignalR sana HubCallerContext verir. Yani Buradaki Context aslında HubCallerContext tipindedir. İçinde ConnectionId,User,UserIdentifier, Items, Features, Abort()... vardır.*/

/*Kod Arkada Nasıl Çalışıyor? (Adım Adım Akış)
Bağlantı Kurulurken (/chathub): Sen arayüzde token'ı girip "Telsize Bağlan" dediğinde, tarayıcı sunucudaki ChatHub sınıfına bağlanır.
Mesaj Gönder butonuna bastığında (SendPrivateMessage): JavaScript kodundaki connection.invoke("SendPrivateMessage", ...) komutu, HTTP üzerinden bir POST isteği atmaz. Bunun yerine, aradaki açık tel (WebSocket) üzerinden sunucuya doğrudan şunu söyler:
"Ey ChatHub, bende SendPrivateMessage adında bir eylem var. İçinde de receiverId ve message verileri var, bunu çalıştır!"
Sonra sunucu tarafında ChatHub.cs'te yani:  token yakalama ve gelen o mesajı kaydetme metoflerı şu şekilde çalışır:
Sunucu, alıcının WebSocket tünelini bulur ve ReceivePrivateMessage komutunu tetikler. Karşı tarafın tarayıcısındaki JS kodu (connection.on("ReceivePrivateMessage", ...)) bunu anında yakalar ve ekrana yeşil mesaj balonunu çizer.*/
