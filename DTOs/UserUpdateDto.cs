using System.ComponentModel.DataAnnotations;

namespace TrendyolMiniApi.DTOs
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Username { get; set; } = string.Empty;
        

        [Required]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
    }
}