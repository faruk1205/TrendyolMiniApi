using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrendyolMiniApi.Controllers
{
    // Ortak Route ve ApiController etiketleri
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected int CurrentUserId => 
            User.Identity?.IsAuthenticated == true
                ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : 0;

        protected string CurrentUsername => 
            User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty
                : string.Empty;

        protected string CurrentUserRole => 
            User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
                : string.Empty;
    }
}