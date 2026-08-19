using System;

namespace TrendyolMiniApi.DTOs
{
    public class GroupMessageResponseDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsMine { get; set; }
    }
}