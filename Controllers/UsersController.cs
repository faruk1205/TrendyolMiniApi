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
        readonly CurrentUser _currentUser;

        public UsersController(IUserService userService, CurrentUser currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }
        
        // 1. PUT: Profil güncelleme. IActionResult ve Ok() sarmalayıcısı kaldırıldı.
        [HttpPut("profile")]
        public async Task<BaseResponseDto> UpdateProfile([FromBody] UserUpdateDto request)
        {
            await _userService.UpdateProfileAsync(request, _currentUser.Id);
            
            return BaseResponseDto.SuccessResult("Profil bilgileriniz başarıyla güncellendi.");
        }

        // 2. DELETE: Hesap silme. Doğrudan BaseResponseDto dönüyoruz.
        [HttpDelete("me")]
        public async Task<BaseResponseDto> DeleteMyAccount()
        {
            await _userService.DeleteMyAccountAsync(_currentUser.Id);
            
            return BaseResponseDto.SuccessResult("Hesabınız ve ona bağlı olan tüm verileriniz başarıyla silindi.");
        }
    }
}