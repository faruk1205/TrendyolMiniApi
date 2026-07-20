using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IUserService
    {
        Task UpdateProfileAsync(UserUpdateDto request, int userId);
        Task DeleteMyAccountAsync(int userId);
    }
}