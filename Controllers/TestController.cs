using Microsoft.AspNetCore.Mvc;
using TrendyolMiniApi.Controllers;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Controllers
{

    public class TestController : BaseApiController
    {
        private readonly CurrentUser _currentUser;
        private readonly ApplicationDbContext _context;

        public TestController( CurrentUser currentUser,ApplicationDbContext context)
        {
            _currentUser = currentUser;
            _context = context;
        }
        [HttpPost("test/join-group")]
        public async Task<IActionResult> TestJoinGroup(int groupId)
        {
            // Sadece geliştirme ortamında kullanılmalı, production'a taşımayın
            _context.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = _currentUser.Id });
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}