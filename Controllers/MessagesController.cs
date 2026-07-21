using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize] // Sadece sisteme giriş yapmış Müşteri veya Satıcılar mesajlarını görebilir
    public class MessagesController : BaseApiController
    {
        [HttpGet("{otherUserId}")]
        public async Task<BaseResponseDto<List<MessageResponseDto>>> GetConversation(int otherUserId, [FromServices] MessageService messageService)
        {
            // 1. İşçiden iki kullanıcı arasındaki mesajlaşma listesini alıyoruz
            var conversation = await messageService.GetConversationAsync(CurrentUserId, otherUserId);

            // 2. Mesaj listesini standart BaseResponseDto çerçevemize sarıp dönüyoruz
            return BaseResponseDto<List<MessageResponseDto>>.SuccessResult(conversation, "Mesajlaşma geçmişi başarıyla getirildi.");
        }
    }
}