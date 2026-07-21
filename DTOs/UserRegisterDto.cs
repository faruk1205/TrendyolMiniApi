using System.ComponentModel.DataAnnotations;
using TrendyolMiniApi.Enums;

namespace TrendyolMiniApi.DTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Kullanıcı adı alanı boş bırakılamaz.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "E-posta alanı boş bırakılamaz.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta formatı girin (Örn: ornek@mail.com).")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Şifre alanı boş bırakılamaz.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakterden olmalıdır.")]
        public string Password { get; set; } = string.Empty;
        
        
        // DİKKAT: Burada C#'a "Sadece Role enum'u içinde tanımlı olan değerleri kabul et!" diyoruz.
        [EnumDataType(typeof(UserRole), ErrorMessage = "Lütfen geçerli bir rol seçiniz (1: Müşteri, 2: Satıcı).")]
        [Required(ErrorMessage = "Rol alanı zorunludur.")]
        public UserRole Role { get; set; } = UserRole.Müşteri;
    }
}