using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Services;

namespace TrendyolMiniApi.Controllers
{
    [Authorize] // GÜVENLİK: Sadece sisteme giriş yapmış Müşteri veya Satıcılar mesajlarını görebilir
    public class MessagesController : BaseApiController
    {
        [HttpGet("{otherUserId}")]
        public async Task<IActionResult> GetConversation(int otherUserId, [FromServices] MessageService messageService)
        {
            return Ok(await messageService.GetConversationAsync(CurrentUserId, otherUserId));
        }
    }
}