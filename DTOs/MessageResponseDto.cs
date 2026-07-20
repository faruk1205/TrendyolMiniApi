namespace TrendyolMiniApi.DTOs
{
    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        
        // SentAt yerine BaseEntity'den gelen doğru ismi koyduk
        public DateTime CreatedDate { get; set; } 
        
        public bool IsRead { get; set; }
        public bool IsMine { get; set; } 
    }
}