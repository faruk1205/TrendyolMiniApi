namespace TrendyolMiniApi.DTOs
{
    public class UserRegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        // Kullanıcı kayıt olurken "Satıcı" mı "Müşteri" mi olacağını seçsin
        public string Role { get; set; } = "Müşteri"; 
    }
}