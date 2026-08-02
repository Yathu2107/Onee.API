using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class WorkerCategoryController(WorkerCategoryService workerCategoryService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/worker-category";
        private readonly WorkerCategoryService _workerCategoryService = workerCategoryService;

        [HttpPost]
        [Route(API_ROUTE_NAME + "/save")]
        public async Task<IActionResult> SaveWorkerCategories([FromBody] WorkerCategoryModelForInsert model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _workerCategoryService.SaveWorkerCategoriesAsync(
                model.FK_user_ID,
                model.Category_ids);

            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{userId}/get-categories")]
        public async Task<IActionResult> GetWorkerCategoriesByUserId(string userId)
        {
            var result = await _workerCategoryService.GetWorkerCategoriesByUserIdAsync(userId);

            return Ok(result);
        }
    }
}
