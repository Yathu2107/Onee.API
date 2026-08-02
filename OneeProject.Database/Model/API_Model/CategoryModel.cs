using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class CategoryModel
    {
        public virtual int Id { get; set; }
        public string Category_Name { get; set; }
        public bool Isdelete { get; set; }
    }

    public class CategoryModelForDetailView : CategoryModel
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public class CategoryModelForInsert : CategoryModel { }

    public class CategoryModelForUpdate : CategoryModel
    {
        [Required]
        public override int Id { get; set; }
    }

    public class CategoryModelForTable
    {
        public int Id { get; set; }
        public string Category_Name { get; set; }
        public bool Isdelete { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class CategoryModelWithPagination
    {
        public List<CategoryModelForTable> Categories { get; set; }
        public int Count { get; set; }
    }

    public class CategoryModelForDropdown
    {
        public int Id { get; set; }
        public string Category_Name { get; set; }
    }
}
