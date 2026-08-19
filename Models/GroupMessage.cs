
using System;
using TrendyolMiniApi.Enums;

namespace TrendyolMiniApi.Models
{
    public class GroupMessage : BaseEntity
    {
        public int SenderId { get; set; }
        public int GroupId { get; set; }
        public string Content { get; set; } = string.Empty;

        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        public int RetryCount { get; set; } = 0;
    }
}