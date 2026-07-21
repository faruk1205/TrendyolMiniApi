using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // 1. REGISTER: IActionResult yerine doğrudan BaseResponseDto dönüyoruz. Ok() sarmalayıcısı yok.
        [HttpPost("register")]
        public async Task<BaseResponseDto> Register(UserRegisterDto request)
        {
            await _authService.RegisterAsync(request);
            
            return BaseResponseDto.SuccessResult("Kullanıcı başarıyla kaydedildi.");
        }

        // 2. LOGIN: Jenerik tipimizi açıkça Task içinde belirtiyoruz ve doğrudan fırlatıyoruz.
        [HttpPost("login")]
        public async Task<BaseResponseDto<string>> Login(UserLoginDto request)
        {
            var token = await _authService.LoginAsync(request);
            
            return BaseResponseDto<string>.SuccessResult(token, "Giriş başarılı!");
        }
        
        // 3. CHANGE PASSWORD: Yine veri dönmeyen, sadece BaseResponseDto dönen temiz yapı.
        [HttpPut("change-password")]
        [Authorize]
        public async Task<BaseResponseDto> ChangePassword([FromBody] PasswordChangeDto request)
        {
            await _authService.ChangePasswordAsync(request, CurrentUserId);
            
            return BaseResponseDto.SuccessResult("Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz.");
        }
    }
}