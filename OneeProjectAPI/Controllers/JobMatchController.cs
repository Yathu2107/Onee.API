using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class JobMatchController(JobMatchService jobMatchService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/job-match";
        private readonly JobMatchService _jobMatchService = jobMatchService;

        /// <summary>
        /// Predict technician category from problem text via AI, then return matching worker profiles
        /// within 7 km of the selected user's location.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/find-workers")]
        public async Task<IActionResult> FindWorkers([FromBody] JobMatchRequestModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Text))
            {
                return BadRequest(new Message<string>
                {
                    Status = "E",
                    Text = "Text is required.",
                    Code = "400",
                    Result = null
                });
            }

            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                return BadRequest(new Message<string>
                {
                    Status = "E",
                    Text = "UserId is required.",
                    Code = "400",
                    Result = null
                });
            }

            var response = await _jobMatchService.FindWorkersByTextAsync(model.Text, model.UserId);

            if (response.Status == "E")
            {
                if (response.Code == "404")
                    return NotFound(response);

                if (response.Code == "400")
                    return BadRequest(response);

                return StatusCode(int.TryParse(response.Code, out var code) ? code : 500, response);
            }

            return Ok(response);
        }
    }
}
