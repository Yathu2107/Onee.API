using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class CategoryService (AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<Message<string>> AddCategoryAsync(Category model)
        {
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            return new Message<string>
            {
                Status = "S",
                Text = "Category added successfully.",
                Code = "200",
                Result = model.Id.ToString()
            };
        }

        public async Task<Category?> CategoryById(int id)
        {
            return await _context.Categories.SingleOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> UpdateCategoryAsync(Category model)
        {
            var existingCategory = await _context.Categories.SingleOrDefaultAsync(p => p.Id == model.Id);

            existingCategory.Category_Name = model.Category_Name;
            existingCategory.Isdelete = model.Isdelete;
            existingCategory.LastUpdatedBy = model.LastUpdatedBy;
            existingCategory.LastUpdatedOn = model.LastUpdatedOn;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CategoryModelForDetailView> CategoryGetById(int id)
        {
            var data = await _context.Categories
                .Where(p => p.Id == id)
                .Select(p => new CategoryModelForDetailView
                {
                    Id = p.Id,
                    Category_Name = p.Category_Name,
                    Isdelete = p.Isdelete,
                    CreatedBy = _context.Users
                        .Where(c => c.Id == p.CreatedBy)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? "",
                    CreatedOn = p.CreatedOn,
                    LastUpdatedBy = _context.Users
                        .Where(c => c.Id == p.LastUpdatedBy)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? "",
                    LastUpdatedOn = p.LastUpdatedOn
                })
                .SingleOrDefaultAsync();
            return data;
        }

        public async Task<CategoryModelWithPagination> GetAllWithPagination(
            int page,
            int items_per_page,
            string search,
            string status)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Category_Name.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();
                if (status == "active") query = query.Where(u => !u.Isdelete);
                else if (status == "inactive") query = query.Where(u => u.Isdelete);
            }

            int totalCount = await query.CountAsync();
            var categories = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * items_per_page)
                .Take(items_per_page)
                .Select(p => new CategoryModelForTable
                {
                    Id = p.Id,
                    Category_Name = p.Category_Name,
                    Isdelete = p.Isdelete,
                    CreatedBy = _context.Users
                        .Where(c => c.Id == p.CreatedBy)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? "",
                    CreatedOn = p.CreatedOn
                })
                .ToListAsync();
            return new CategoryModelWithPagination
            {
                Count = totalCount,
                Categories = categories
            };
        }

        public async Task<List<CategoryModelForDropdown>> CategoryGetAllForList()
        {
            var data = await _context.Categories
                .Where(p => !p.Isdelete)
                .Select(p => new CategoryModelForDropdown
                {
                    Id = p.Id,
                    Category_Name = p.Category_Name
                })
                .ToListAsync();
            return data;
        }
    }
}
