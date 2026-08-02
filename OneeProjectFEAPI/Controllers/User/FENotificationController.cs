using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FENotificationController(FENotificationService notificationService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/notifications";
        private readonly FENotificationService _notificationService = notificationService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpGet]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> Mine()
        {
            var list = await _notificationService.GetMineAsync(CurrentUserId);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Notifications loaded.",
                Code = "200",
                Result = list
            });
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Unread count loaded.",
                Code = "200",
                Result = count
            });
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var result = await _notificationService.MarkReadAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var result = await _notificationService.MarkAllReadAsync(CurrentUserId);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
