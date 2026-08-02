using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.FEAPI_Model.Worker;
using OneeProject.Services.FeServices.Worker;

namespace OneeProjectFEAPI.Controllers.Worker
{
    [Authorize]
    [ApiController]
    public class FEWorkerJobController(FEWorkerJobService workerJobService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/worker/jobs";
        private readonly FEWorkerJobService _workerJobService = workerJobService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpGet]
        [Route(API_ROUTE_NAME + "/offers")]
        public async Task<IActionResult> Offers()
        {
            var offers = await _workerJobService.GetOffersAsync(CurrentUserId);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Offers loaded.",
                Code = "200",
                Result = offers
            });
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/mine")]
        public async Task<IActionResult> Mine()
        {
            var jobs = await _workerJobService.GetMineAsync(CurrentUserId);
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
            var result = await _workerJobService.GetAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var result = await _workerJobService.AcceptAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            var result = await _workerJobService.DeclineAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/confirm")]
        public async Task<IActionResult> Confirm(int id, [FromBody] FEWorkerJobConfirmRequest model)
        {
            var result = await _workerJobService.ConfirmAsync(id, CurrentUserId, model.Amount);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _workerJobService.CompleteAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/chat")]
        public async Task<IActionResult> SendChat(int id, [FromBody] FEWorkerJobChatSendRequest model)
        {
            var result = await _workerJobService.SendChatAsync(id, CurrentUserId, model.Message);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}/chat")]
        public async Task<IActionResult> GetChat(int id)
        {
            var result = await _workerJobService.GetChatAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
