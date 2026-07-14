namespace TrendyolMiniApi.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        public int ReceiverId { get; set; }
        public User? Receiver { get; set; }
    }
}