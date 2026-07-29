using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize] // Sadece sisteme giriş yapmış Müşteri veya Satıcılar mesajlarını görebilir
    public class MessagesController : BaseApiController
    {

        private readonly CurrentUser _currentUser;

        public MessagesController(CurrentUser currentUser)
        {
            _currentUser = currentUser;
        }
        
        [HttpGet("{otherUserId}")]
        public async Task<BaseResponseDto<List<MessageResponseDto>>> GetConversation(int otherUserId, [FromServices] MessageService messageService)
        {
            // 1. İşçiden iki kullanıcı arasındaki mesajlaşma listesini alıyoruz
            var conversation = await messageService.GetConversationAsync(_currentUser.Id, otherUserId);

            // 2. Mesaj listesini standart BaseResponseDto çerçevemize sarıp dönüyoruz
            return BaseResponseDto<List<MessageResponseDto>>.SuccessResult(conversation, "Mesajlaşma geçmişi başarıyla getirildi.");
        }
    }
}