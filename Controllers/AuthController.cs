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

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            await _authService.RegisterAsync(request);
            return Ok(new { Message = "Kullanıcı başarıyla kaydedildi." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            var token = await _authService.LoginAsync(request);
            return Ok(new { Token = token, Message = "Giriş başarılı!" });
        }
        
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeDto request)
        {
            await _authService.ChangePasswordAsync(request, CurrentUserId);
            return Ok(new { Message = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz." });
        }
    }
}