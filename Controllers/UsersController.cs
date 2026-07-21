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
        
        // 1. PUT: Profil güncelleme. IActionResult ve Ok() sarmalayıcısı kaldırıldı.
        [HttpPut("profile")]
        public async Task<BaseResponseDto> UpdateProfile([FromBody] UserUpdateDto request)
        {
            await _userService.UpdateProfileAsync(request, CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Profil bilgileriniz başarıyla güncellendi.");
        }

        // 2. DELETE: Hesap silme. Doğrudan BaseResponseDto dönüyoruz.
        [HttpDelete("me")]
        public async Task<BaseResponseDto> DeleteMyAccount()
        {
            await _userService.DeleteMyAccountAsync(CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Hesabınız ve ona bağlı olan tüm verileriniz başarıyla silindi.");
        }
    }
}