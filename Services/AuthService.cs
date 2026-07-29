using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Enums;
using TrendyolMiniApi.Models;
using TrendyolMiniApi.Markers;
using Microsoft.IdentityModel.JsonWebTokens;


namespace TrendyolMiniApi.Services
{
    public class AuthService : IAuthService , IScopedService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task RegisterAsync(UserRegisterDto request)
        {
            
            // Gelen rakam (örneğin 5), bizim Role enum'umuzun içinde var mı diye kontrol ediyoruz.
            if (!Enum.IsDefined(typeof(UserRole), request.Role))
            {
                // Yoksa, Akıllı Kalkanımızın (GlobalExceptionHandler) 400 Bad Request'e çevireceği hatayı fırlatıyoruz!
                throw new InvalidOperationException("Sisteme kayıt olurken geçersiz bir rol (yetki) gönderdiniz.");
            }
            
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Bu e-posta adresi zaten kullanımda.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = request.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<string> LoginAsync(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Yanlış şifre.");
            }

            return CreateToken(user);
        }

        public async Task ChangePasswordAsync(PasswordChangeDto request, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) 
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isOldPasswordCorrect)
                throw new UnauthorizedAccessException("Mevcut şifrenizi yanlış girdiniz.");

            if (request.CurrentPassword == request.NewPassword)
                throw new InvalidOperationException("Yeni şifreniz, eski şifreniz ile aynı olamaz.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
        }
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // 1. İmzalama Anahtarı (Tokenın değiştirilmediğini doğrular)
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtSettings:Secret").Value!));
    
            var signingCreds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            // 2. Şifreleme Anahtarı (Claim verilerini okunmaz hale getirir - Mutlaka 32 byte/karakter olmalı)
            var encryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtSettings:EncryptionKey").Value!));
        
            
            //Bu satır, JWT'nin okunamaması için hangi anahtar ve hangi şifreleme algoritmalarıyla şifreleneceğini belirler.
            var encryptingCreds = new EncryptingCredentials(encryptionKey, 
                SecurityAlgorithms.Aes256KW, 
                SecurityAlgorithms.Aes256CbcHmacSha512);
            //JwtKeyWrapAlgorithms.Aes256 anahtarın nasıl korunacağını belirleyen algoritmadır
            //SecurityAlgorithms.Aes256CbcHmacSha512 bu ise veriyi gerçekten şifreleyen algoritmadır
            

            // 3. Token Tanımlayıcı (Modern yaklaşım)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims), //claim listesi buraya geliyor.JWT payloadı buradan oluştuturur.
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = _configuration.GetSection("JwtSettings:Issuer").Value,
                Audience = _configuration.GetSection("JwtSettings:Audience").Value,
                SigningCredentials = signingCreds,
                EncryptingCredentials = encryptingCreds // !!!!!Kritik yer burası 
                //Bu satırı eklediğinde JWT oluşturulurken token JWE olarak şifrelenir. encryptingCreds ise bir önceki satırda hangi anahtar ve algoritmalarla şifreleneceğini belirleyen bilgileri taşır.
                //normalde JWT'de header->payload->imzalanır->token   OLUŞURDU AMA BURADA   claimler->imzalanır-> şifrelenir-> token oluşur
            };

            // 4. Token'ı Oluştur
            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(tokenDescriptor);
        }
       
    }
}

/*private string CreateToken(User user)
       {
           var claims = new List<Claim>
           {
               new Claim(ClaimTypes.Name, user.Username),
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Role, user.Role.ToString())
           };

           var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
               _configuration.GetSection("JwtSettings:Secret").Value!));

           var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);  

           var token = new JwtSecurityToken(
               issuer: _configuration.GetSection("JwtSettings:Issuer").Value,
               audience: _configuration.GetSection("JwtSettings:Audience").Value,
               claims: claims,
               expires: DateTime.Now.AddDays(1),
               signingCredentials: creds
           );

           return new JwtSecurityTokenHandler().WriteToken(token);
       }*/

//buradaki amaç sunucumdaki secret'ımla key oluşturup. Kullanıcı bilgisiyle bu keyi alıp öyle tokenı imzalamak. Bu sayede dışarıdan tokena erişsen biri keyi asla bilemediği için tokenı görüntüleyebilir ama asle değiştiremez.
// dışarıdan biri tokendan bilgileri görüntüleyebiliyor çünkü jwt payload kısmı şifrelenmez, sadece imzalanır. Yani token'ın değiştirilmediği garanti edilir ama içindeki bilgiler gizli değildir.
//JWT 3 parçadan oluşur Header, Payload, Signature 
// Header ilk kısım algoritma bilgisini içerir : 
    /*{
  "alg": "HS512",
  "typ": "JWT"
    }*/
//Payload ikinci kısım kullanıcı bilgilerini içerir.:
    /*{
        "sub": "123",
        "name": "Ahmet",
        "role": "Admin"
    }*/
//üçüncü kısım signature, bu da bizim kodda oluşturduğumuz imzadır. Yaklaşık olarak bu mantıkla hesaplanır. Buradaki secret key sadece sunucuda bulunur.
    /*
    HMACSHA512(
    Header + "." + Payload,
    SecretKey
    )
    */
//Peki biri payload'ı okuyabiliyorsa güvenlik açığı yok mu? Hayır. Çünkü JWT'nin amacı veriyi gizlemek değil, verinin değiştirilmediğini kanıtlamaktır.İşte değiştirelememesinin sebebide imza mantığı.
      
       