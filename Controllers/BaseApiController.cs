using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrendyolMiniApi.Controllers
{
    // Ortak Route ve ApiController etiketleri
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
    }
}