using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Enums;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class MessageService : IMessageService, IScopedService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MessageResponseDto>> GetConversationAsync(int currentUserId, int otherUserId)
        {
            // --- Mevcut 1-1 mesajlaşma mantığınız, hiç dokunmadım ---
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

        public async Task<List<GroupMessageResponseDto>> GetGroupConversationAsync(
            int groupId, int currentUserId, int? cursor, int pageSize)
        {
            // Üye değilse geçmişi görmesin. Controller'a "yetkisiz" olarak
            // yansıtmak için burada özel bir exception fırlatıyoruz.
            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedAccessException("Bu gruba üye değilsiniz.");

            pageSize = Math.Clamp(pageSize, 1, 100); //"pageSize 1'den küçükse 1 yap, 100'den büyükse 100 yap, aradaysa olduğu gibi bırak."

            var query = _context.GroupMessages
                .Where(m => m.GroupId == groupId && m.Status != MessageStatus.Failed);

            if (cursor.HasValue)
                query = query.Where(m => m.Id < cursor.Value); //"Bana cursor olarak verilen mesaj ID'sinden daha eski mesajları getir." Cursor, "buraya kadar geldim; bundan daha eski/yeni kayıtları getir" diye backend'e verilen referans noktasıdır.

            var messages = await query
                .OrderByDescending(m => m.Id)
                .Take(pageSize)
                .Select(m => new GroupMessageResponseDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    GroupId = m.GroupId,
                    Content = m.Content,
                    CreatedDate = m.CreatedDate,
                    IsMine = m.SenderId == currentUserId
                })
                .ToListAsync();

            // Cursor ile geldiği için sonucu tekrar kronolojik sıraya çeviriyoruz
            messages.Reverse();

            return messages;
        }
    }
}