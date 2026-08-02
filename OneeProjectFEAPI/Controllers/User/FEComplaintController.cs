using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Model.FEAPI_Model.User;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FEComplaintController(FEComplaintService complaintService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/complaints";
        private readonly FEComplaintService _complaintService = complaintService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpPost]
        [Route(API_ROUTE_NAME + "/create")]
        public async Task<IActionResult> Create([FromBody] FEComplaintCreateRequest model)
        {
            var result = await _complaintService.CreateAsync(model, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
