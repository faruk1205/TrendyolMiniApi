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
        public async Task<BaseResponseDto<List<MessageResponseDto>>> GetConversation(
            int otherUserId, [FromServices] MessageService messageService)
        {
            var conversation = await messageService.GetConversationAsync(_currentUser.Id, otherUserId);
            return BaseResponseDto<List<MessageResponseDto>>.SuccessResult(conversation, "Mesajlaşma geçmişi başarıyla getirildi.");
        }

        
        
        // GET api/messages/group/3?cursor=980&pageSize=50
        [HttpGet("group/{groupId}")]
        public async Task<BaseResponseDto<List<GroupMessageResponseDto>>> GetGroupConversation(
            int groupId,
            [FromServices] MessageService messageService,
            [FromQuery] int? cursor = null,
            [FromQuery] int pageSize = 50)
        {
            
                var conversation = await messageService.GetGroupConversationAsync(
                    groupId, _currentUser.Id, cursor, pageSize);

                return BaseResponseDto<List<GroupMessageResponseDto>>.SuccessResult(
                    conversation, "Grup mesajlaşma geçmişi başarıyla getirildi.");
            
           
        }
    }
}