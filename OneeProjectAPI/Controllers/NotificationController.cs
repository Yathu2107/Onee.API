using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class NotificationController(NotificationService notificationService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/notification";
        private readonly NotificationService _notificationService = notificationService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpPost]
        [Route(API_ROUTE_NAME + "/create")]
        public async Task<IActionResult> Create([FromBody] AdminNotificationModelForInsert model)
        {
            var result = await _notificationService.CreateAdminBroadcastAsync(model, CurrentUserId);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/list")]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int items_per_page = 10,
            [FromQuery] string? status = null)
        {
            var result = await _notificationService.GetAdminBroadcastsWithPagination(
                page, items_per_page, status);
            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var view = await _notificationService.GetAdminBroadcastByIdAsync(id);
            if (view == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Notification not found.",
                    Code = "404"
                });
            }
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Notification loaded.",
                Code = "200",
                Result = view
            });
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/send")]
        public async Task<IActionResult> Send(int id)
        {
            var result = await _notificationService.SendAdminBroadcastAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/inbox")]
        public async Task<IActionResult> Inbox(
            [FromQuery] int page = 1,
            [FromQuery] int items_per_page = 10,
            [FromQuery] string? userId = null,
            [FromQuery] string? type = null)
        {
            var result = await _notificationService.GetInboxWithPagination(
                page, items_per_page, userId, type);
            return Ok(result);
        }
    }
}
