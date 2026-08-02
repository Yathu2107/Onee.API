using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FECategoryController(FECategoryService categoryService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/categories";
        private readonly FECategoryService _categoryService = categoryService;

        /// <summary>
        /// Active categories from Admin panel for User app manual browse/find flow.
        /// </summary>
        [HttpGet]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> ListActive()
        {
            var list = await _categoryService.GetActiveAsync();
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Categories loaded.",
                Code = "200",
                Result = list
            });
        }
    }
}
