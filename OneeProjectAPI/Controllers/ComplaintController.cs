using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class ComplaintController(ComplaintService complaintService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/complaint";
        private readonly ComplaintService _complaintService = complaintService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpGet]
        [Route(API_ROUTE_NAME + "/list")]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int items_per_page = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var result = await _complaintService.GetAllWithPagination(
                page, items_per_page, status, search);
            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var view = await _complaintService.GetByIdAsync(id);
            if (view == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Complaint not found.",
                    Code = "404"
                });
            }
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Complaint loaded.",
                Code = "200",
                Result = view
            });
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id:int}/update-status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] ComplaintModelForUpdateStatus model)
        {
            var result = await _complaintService.UpdateStatusAsync(id, model, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
