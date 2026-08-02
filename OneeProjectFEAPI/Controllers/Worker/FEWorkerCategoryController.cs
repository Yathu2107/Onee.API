using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.FEAPI_Model.Worker;
using OneeProject.Services.FeServices.Worker;

namespace OneeProjectFEAPI.Controllers.Worker
{
    [Authorize]
    [ApiController]
    public class FEWorkerCategoryController(FEWorkerCategoryService categoryService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/worker/categories";
        private readonly FEWorkerCategoryService _categoryService = categoryService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        /// <summary>
        /// Active categories from Admin panel (skills the worker can select).
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> ListActive()
        {
            var list = await _categoryService.GetActiveCategoriesAsync();
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Categories loaded.",
                Code = "200",
                Result = list
            });
        }

        /// <summary>
        /// Categories currently selected by the logged-in worker.
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/mine")]
        public async Task<IActionResult> Mine()
        {
            var result = await _categoryService.GetMineAsync(CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Replace the logged-in worker's selected categories (skills).
        /// Uses admin Category list IDs only.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> Save([FromBody] FEWorkerSaveCategoriesRequest model)
        {
            var result = await _categoryService.SaveMineAsync(
                CurrentUserId,
                model?.CategoryIds);
            if (result.Code == "404") return NotFound(result);
            if (result.Code == "403") return StatusCode(403, result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
