using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services
{
    public class UserService : IUserService , IScopedService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateProfileAsync(UserUpdateDto request, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) 
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            if (user.Email != request.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                    throw new InvalidOperationException("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            }

            user.Username = request.Username;
            user.Email = request.Email;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteMyAccountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Trendyol kuralı: Fatura veya mesaj geçmişi olduğu için silinemez!
                throw new InvalidOperationException("Hesabınız silinemiyor. Geçmişe dönük silinemez kayıtlarınız (Sipariş faturaları veya mesaj geçmişi) bulunmaktadır.");
            }
        }
    }
}