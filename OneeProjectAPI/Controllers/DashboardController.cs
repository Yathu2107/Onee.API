using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class DashboardController(DashboardService dashboardService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/dashboard";
        private readonly DashboardService _dashboardService = dashboardService;

        /// <summary>
        /// Dashboard count cards + chart series for first page load.
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/overview")]
        public async Task<IActionResult> Overview()
        {
            var data = await _dashboardService.GetOverviewAsync();
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Dashboard overview loaded.",
                Code = "200",
                Result = data
            });
        }

        /// <summary>
        /// Jobs created over time for bar chart. days clamped to 7–90 (default 30).
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/jobs-trend")]
        public async Task<IActionResult> JobsTrend([FromQuery] int days = 30)
        {
            var data = await _dashboardService.GetJobsTrendAsync(days);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Jobs trend loaded.",
                Code = "200",
                Result = data
            });
        }
    }
}
