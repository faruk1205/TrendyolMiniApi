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
        }
    }
}