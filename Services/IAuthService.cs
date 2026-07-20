using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(UserRegisterDto request);
        Task<string> LoginAsync(UserLoginDto request);
        Task ChangePasswordAsync(PasswordChangeDto request, int userId);
    }
}