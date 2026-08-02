using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class CategoryController(CategoryService categoryService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/category";
        private readonly CategoryService _categoryService = categoryService;

        #region Version 1.0 APIs

        /// <summary>
        /// Add a new category to the system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/add-category")]
        public async Task<IActionResult> AddCategory([FromBody] CategoryModelForInsert model)
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            var response = await _categoryService.AddCategoryAsync(new Category()
            {
                Category_Name = model.Category_Name,
                Isdelete = model.Isdelete,
                CreatedOn = CommonResources.LocalDatetime(),
                CreatedBy = userId,
                LastUpdatedOn = CommonResources.LocalDatetime(),
                LastUpdatedBy = userId,
            });
            return Ok(response);
        }

        /// <summary>
        /// Update an existing category by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id}/update-category")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryModelForUpdate model)
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            var dbCategory = await _categoryService.CategoryById(id);
            if (dbCategory == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Category not found.",
                    Code = "404",
                    Result = null
                });
            }

            dbCategory.Category_Name = model.Category_Name;
            dbCategory.Isdelete = model.Isdelete;
            dbCategory.LastUpdatedOn = CommonResources.LocalDatetime();
            dbCategory.LastUpdatedBy = userId;

            if (!await _categoryService.UpdateCategoryAsync(dbCategory))
            {
                return BadRequest(new Message<string>()
                {
                    Status = "E",
                    Text = "Failed to update category.",
                    Code = "400",
                    Result = string.Empty
                });
            }
            return Ok(new Message<string>()
            {
                Status = "S",
                Text = "Category updated successfully.",
                Code = "200",
                Result = string.Empty
            });
        }

        /// <summary>
        /// Get a category by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id}/get-category")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var data = await _categoryService.CategoryGetById(id);
            if (data == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Category not found.",
                    Code = "404",
                    Result = null
                });
            }
            return Ok(data);
        }

        /// <summary>
        /// Get all categories with pagination, search, and status filtering.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="items_per_page">The number of items per page.</param>
        /// <param name="search">The search term to filter categories.</param>
        /// <param name="status">The status to filter categories.</param>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all")]
        public async Task<IActionResult> GetAllWithPagination(
            int page = 1,
            int items_per_page = 10,
            string search = null,
            string status = null)
        {
            var data = await _categoryService.GetAllWithPagination(page, items_per_page, search, status);

            var paginationHelper = new FEPaginationHelper<CategoryModelForTable>(items_per_page, data.Count);
            var paginationInfo = paginationHelper.GetPaginationInfo(page);

            var payload = new Payload
            {
                Pagination = paginationInfo
            };

            var response = new DataResponse<List<CategoryModelForTable>>
            {
                Data = data.Categories,
                Payload = payload
            };

            return Ok(response);
        }

        /// <summary>
        /// Get all categories for a dropdown list or selection.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all-for-list")]
        public async Task<IActionResult> GetAllForList()
        {
            var data = await _categoryService.CategoryGetAllForList();
            if (data == null || !data.Any())
            {
                return NotFound(new Message<string>
                {
                    Text = "No categories found.",
                    Code = "404",
                });
            }
            return Ok(data);
        }
        #endregion
    }
}
