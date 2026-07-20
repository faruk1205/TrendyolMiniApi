using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;

namespace TrendyolMiniApi.Services
{
    public class MessageService 
    {
        private readonly ApplicationDbContext _context;

        // Veritabanı ile konuşma yetkisini servise veriyoruz
        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MessageResponseDto>> GetConversationAsync(int currentUserId, int otherUserId)
        {
            // 1. SOHBET GEÇMİŞİNİ GETİR
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) || 
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.CreatedDate)
                .Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    CreatedDate = m.CreatedDate,
                    IsRead = m.IsRead,
                    IsMine = m.SenderId == currentUserId
                })
                .ToListAsync();

            // 2. GÖRÜLDÜ ÖZELLİĞİ
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return messages;
        }
    }
}