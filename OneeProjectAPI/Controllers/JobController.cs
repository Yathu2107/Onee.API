using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    /// <summary>
    /// Admin panel job APIs. Poll GET endpoints for status/chat updates (no SignalR).
    /// </summary>
    [Authorize]
    [ApiController]
    public class JobController(JobService jobService, JobRatingService jobRatingService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/job";
        private readonly JobService _jobService = jobService;
        private readonly JobRatingService _jobRatingService = jobRatingService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        /// <summary>
        /// Create job with ordered worker queue. First worker gets a 1-minute offer.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/create")]
        public async Task<IActionResult> CreateJob([FromBody] JobCreateModel model)
        {
            var response = await _jobService.CreateJobAsync(model, CurrentUserId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Admin accepts current offer → Accepted (chat opens).
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/accept")]
        public async Task<IActionResult> AcceptOffer(int id)
        {
            var response = await _jobService.AcceptOfferAsync(id, CurrentUserId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Admin cancels current offer → immediately next queued worker.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/cancel-offer")]
        public async Task<IActionResult> CancelOffer(int id)
        {
            var response = await _jobService.CancelOfferAsync(id, CurrentUserId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Admin confirms job with amount → Ongoing.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/confirm")]
        public async Task<IActionResult> ConfirmJob(int id, [FromBody] JobConfirmModel model)
        {
            var response = await _jobService.ConfirmJobAsync(id, CurrentUserId, model.Amount);
            return ToActionResult(response);
        }

        /// <summary>
        /// Admin cancels after accept → Cancelled (job ends, no next worker).
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/cancel")]
        public async Task<IActionResult> CancelAcceptedJob(int id, [FromBody] JobCancelModel model)
        {
            var response = await _jobService.CancelAcceptedJobAsync(id, CurrentUserId, model.Reason);
            return ToActionResult(response);
        }

        /// <summary>
        /// Admin marks job completed → Completed.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/complete")]
        public async Task<IActionResult> CompleteJob(int id)
        {
            var response = await _jobService.CompleteJobAsync(id, CurrentUserId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Poll this for live status / current worker / offer expiry / messages.
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id}/get-job")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var data = await _jobService.GetJobDetailAsync(id);
            if (data == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                });
            }
            return Ok(data);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all")]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int items_per_page = 10,
            string search = null,
            string status = null)
        {
            var data = await _jobService.GetAllWithPagination(page, items_per_page, search, status);
            var paginationHelper = new FEPaginationHelper<JobModelForTable>(items_per_page, data.Count);

            return Ok(new DataResponse<List<JobModelForTable>>
            {
                Data = data.Jobs,
                Payload = new Payload { Pagination = paginationHelper.GetPaginationInfo(page) }
            });
        }

        /// <summary>
        /// Admin-mediated chat. Pass SenderId as customer or assigned worker.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/chat/send")]
        public async Task<IActionResult> SendChat(int id, [FromBody] JobChatSendModel model)
        {
            var response = await _jobService.SendChatMessageAsync(id, model.SenderId, model.Message);
            if (response.Status == "E")
            {
                if (response.Code == "404") return NotFound(response);
                if (response.Code == "403") return StatusCode(403, response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id}/chat/messages")]
        public async Task<IActionResult> GetChatMessages(int id)
        {
            var job = await _jobService.GetJobDetailAsync(id);
            if (job == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                });
            }

            var messages = await _jobService.GetChatMessagesAsync(id);
            return Ok(messages);
        }

        /// <summary>
        /// Add one-time rating/feedback for a completed job (cannot be changed later).
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id}/rating")]
        public async Task<IActionResult> AddRating(int id, [FromBody] JobRatingModelForInsert model)
        {
            var response = await _jobRatingService.AddRatingAsync(id, model, CurrentUserId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Get the rating for a job (if submitted).
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id}/rating")]
        public async Task<IActionResult> GetRating(int id)
        {
            var data = await _jobRatingService.GetByJobIdAsync(id);
            if (data == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Rating not found for this job.",
                    Code = "404"
                });
            }
            return Ok(data);
        }

        private IActionResult ToActionResult<T>(Message<T> response)
        {
            if (response.Status == "E")
            {
                if (response.Code == "404") return NotFound(response);
                if (response.Code == "403") return StatusCode(403, response);
                if (response.Code == "400") return BadRequest(response);
                return StatusCode(int.TryParse(response.Code, out var code) ? code : 500, response);
            }
            return Ok(response);
        }
    }
}
