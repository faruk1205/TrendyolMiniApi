using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto request)
        {
            await _userService.UpdateProfileAsync(request, CurrentUserId);
            return Ok(new { Message = "Profil bilgileriniz başarıyla güncellendi." });
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            await _userService.DeleteMyAccountAsync(CurrentUserId);
            return Ok(new { Message = "Hesabınız ve ona bağlı olan (izin verilen) tüm verileriniz başarıyla silindi." });
        }
    }
}