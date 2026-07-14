using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. KAYIT OLMA (REGISTER)
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            // E-posta daha önce kullanılmış mı kontrol et
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Bu e-posta adresi zaten kullanımda.");
            }

            // Şifreyi BCrypt ile kriptola (Hashle)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Yeni kullanıcıyı oluştur
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = request.Role // "Satıcı" veya "Müşteri"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı başarıyla kaydedildi.");
        }

        // 2. GİRİŞ YAPMA VE JWT ÜRETME (LOGIN)
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // Kullanıcıyı veritabanında bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("Kullanıcı bulunamadı.");
            }

            // Şifre doğru mu kontrol et
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Yanlış şifre.");
            }

            // Şifre doğruysa JWT Biletini (Token) Hazırla
            var token = CreateToken(user);

            return Ok(new { Token = token, Message = "Giriş başarılı!" });
        }

        // JWT (BİLET) ÜRETME MOTORU
        private string CreateToken(User user)
        {
            // Biletin içine kullanıcının adını ve ROLÜNÜ (Satıcı/Müşteri) gizlice yazıyoruz
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role) // En kritik satır burası!
            };

            // appsettings.json'dan gizli anahtarı al
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtSettings:Secret").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("JwtSettings:Issuer").Value,
                audience: _configuration.GetSection("JwtSettings:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Bilet 1 gün geçerli
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}