using System;

namespace TrendyolMiniApi.Models
{
    public class GroupMember : BaseEntity
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}