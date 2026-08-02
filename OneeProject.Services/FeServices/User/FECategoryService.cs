using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.User
{
    public class FECategoryService(CategoryService categoryService)
    {
        private readonly CategoryService _categoryService = categoryService;

        public Task<List<CategoryModelForDropdown>> GetActiveAsync()
            => _categoryService.CategoryGetAllForList();
    }
}
