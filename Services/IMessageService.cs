using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public interface IMessageService
    {
        Task<List<MessageResponseDto>> GetConversationAsync(int currentUserId, int otherUserId);
    }
}