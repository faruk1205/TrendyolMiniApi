using TrendyolMiniApi.Markers;

public class CurrentUser : IScopedService
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    
    // Kullanıcının giriş yapıp yapmadığını kontrol etmek için pratik bir property
    public bool IsAuthenticated => Id > 0; 
}