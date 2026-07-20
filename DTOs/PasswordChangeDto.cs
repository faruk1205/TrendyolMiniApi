using System.ComponentModel.DataAnnotations;

namespace TrendyolMiniApi.DTOs
{
    public class PasswordChangeDto
    {
        [Required(ErrorMessage = "Mevcut şifrenizi girmelisiniz.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Yeni şifreniz en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}