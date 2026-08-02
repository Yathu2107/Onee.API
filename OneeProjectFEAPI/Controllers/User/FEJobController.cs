using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.FEAPI_Model.User;
using OneeProject.Services.FeServices;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FEJobController(FEJobService jobService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/jobs";
        private readonly FEJobService _jobService = jobService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpPost]
        [Route(API_ROUTE_NAME + "/find-workers")]
        public async Task<IActionResult> FindWorkers([FromBody] FEJobFindWorkersRequest model)
        {
            var result = await _jobService.FindWorkersAsync(model.Text, CurrentUserId);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/create")]
        public async Task<IActionResult> Create([FromBody] FEJobCreateRequest model)
        {
            var result = await _jobService.CreateAsync(model, CurrentUserId);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/mine")]
        public async Task<IActionResult> Mine()
        {
            var jobs = await _jobService.GetMineAsync(CurrentUserId);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Jobs loaded.",
                Code = "200",
                Result = jobs
            });
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _jobService.GetAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Customer cancel. Allowed while job is Offering (before worker accepts) or Accepted.
        /// Reason is optional during Offering; required after Accepted.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] FEJobCancelRequest? model)
        {
            var result = await _jobService.CancelAsync(id, CurrentUserId, model?.Reason ?? string.Empty);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/chat")]
        public async Task<IActionResult> SendChat(int id, [FromBody] FEJobChatSendRequest model)
        {
            var result = await _jobService.SendChatAsync(id, CurrentUserId, model.Message);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}/chat")]
        public async Task<IActionResult> GetChat(int id)
        {
            var result = await _jobService.GetChatAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/rating")]
        public async Task<IActionResult> Rating(int id, [FromBody] FEJobRatingRequest model)
        {
            var result = await _jobService.AddRatingAsync(id, CurrentUserId, model);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
