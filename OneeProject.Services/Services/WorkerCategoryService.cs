using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class WorkerCategoryService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<Message<string>> SaveWorkerCategoriesAsync(
        string userId,
        List<int> categoryIds)
        {
            var worker = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (worker == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "404"
                };
            }

            if (!string.Equals(worker.UserType, "Worker", StringComparison.OrdinalIgnoreCase))
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Categories can only be assigned to Worker accounts.",
                    Code = "400"
                };
            }

            // Remove duplicates
            categoryIds = (categoryIds ?? new List<int>()).Distinct().ToList();

            // Validate categories
            var validCategoryIds = await _context.Categories
                .Where(c => categoryIds.Contains(c.Id) && !c.Isdelete)
                .Select(c => c.Id)
                .ToListAsync();

            var existing = await _context.WorkerCategories
                .Where(x => x.FK_user_ID == userId)
                .ToListAsync();

            // Delete removed categories
            var removeList = existing
                .Where(x => !validCategoryIds.Contains(x.Category_id))
                .ToList();

            _context.WorkerCategories.RemoveRange(removeList);

            // Insert new categories
            var existingIds = existing.Select(x => x.Category_id).ToHashSet();

            var addList = validCategoryIds
                .Where(id => !existingIds.Contains(id))
                .Select(id => new WorkerCategory
                {
                    FK_user_ID = userId,
                    Category_id = id
                });

            await _context.WorkerCategories.AddRangeAsync(addList);

            await _context.SaveChangesAsync();

            return new Message<string>
            {
                Status = "S",
                Text = "Worker categories updated successfully.",
                Code = "200",
                Result = validCategoryIds.Count.ToString()
            };
        }


        public async Task<Message<List<WorkerCategoryModelForView>>> GetWorkerCategoriesByUserIdAsync(string userId)
        {
            var worker = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (worker == null)
            {
                return new Message<List<WorkerCategoryModelForView>>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "404",
                    Result = null
                };
            }

            var categories = await _context.WorkerCategories
                .Where(wc => wc.FK_user_ID == userId)
                .Join(
                    _context.Categories.Where(c => !c.Isdelete),
                    wc => wc.Category_id,
                    c => c.Id,
                    (wc, c) => new WorkerCategoryModelForView
                    {
                        Category_id = c.Id,
                        Category_Name = c.Category_Name
                    })
                .OrderBy(x => x.Category_Name)
                .ToListAsync();

            return new Message<List<WorkerCategoryModelForView>>
            {
                Status = "S",
                Text = "Worker categories retrieved successfully.",
                Code = "200",
                Result = categories
            };
        }
    }
}
