using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.Worker
{
    public class FEWorkerCategoryService(
        AppDbContext context,
        CategoryService categoryService,
        WorkerCategoryService workerCategoryService)
    {
        private readonly AppDbContext _context = context;
        private readonly CategoryService _categoryService = categoryService;
        private readonly WorkerCategoryService _workerCategoryService = workerCategoryService;

        public Task<List<CategoryModelForDropdown>> GetActiveCategoriesAsync()
            => _categoryService.CategoryGetAllForList();

        public async Task<Message<List<WorkerCategoryModelForView>>> GetMineAsync(string workerId)
        {
            var guard = await EnsureWorkerAsync(workerId);
            if (guard != null)
            {
                return new Message<List<WorkerCategoryModelForView>>
                {
                    Status = "E",
                    Text = guard.Text,
                    Code = guard.Code,
                    Result = null
                };
            }

            return await _workerCategoryService.GetWorkerCategoriesByUserIdAsync(workerId);
        }

        public async Task<Message<string>> SaveMineAsync(string workerId, List<int>? categoryIds)
        {
            var guard = await EnsureWorkerAsync(workerId);
            if (guard != null)
                return guard;

            return await _workerCategoryService.SaveWorkerCategoriesAsync(
                workerId,
                categoryIds ?? new List<int>());
        }

        private async Task<Message<string>?> EnsureWorkerAsync(string workerId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == workerId);
            if (user == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "404",
                    Result = string.Empty
                };
            }

            if (!string.Equals(user.UserType, "Worker", StringComparison.OrdinalIgnoreCase))
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Only workers can manage skill categories.",
                    Code = "403",
                    Result = string.Empty
                };
            }

            return null;
        }
    }
}
