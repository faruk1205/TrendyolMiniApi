using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyolMiniApi.Models
{
    public class Message : BaseEntity
    {
        [Required]
        public int SenderId { get; set; } // Mesajı gönderen kişi

        [Required]
        public int ReceiverId { get; set; } // Mesajı alan kişi (Müşteri veya Satıcı)

        [Required]
        public string Content { get; set; } = string.Empty;
        
        public bool IsRead { get; set; } = false; // "Görüldü" özelliği için

        // === EF CORE NAVİGASYON ÖZELLİKLERİ ===
        // Bu özellikler veritabanında kolon olmaz, sadece Join işlemlerinde hayat kurtarır.
        
        [ForeignKey("SenderId")]
        public User? Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public User? Receiver { get; set; }
    }
}