using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class WorkerCategoryModel
    {
        public string FK_user_ID { get; set; } = string.Empty;
        public int Category_id { get; set; }
    }

    public class WorkerCategoryModelForInsert
    {
        [Required]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required]
        public List<int> Category_ids { get; set; } = new();
    }

    public class WorkerCategoryModelForUpdate
    {
        [Required]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required]
        public List<int> Category_ids { get; set; } = new();
    }

    public class WorkerCategoryModelForView
    {
        public int Category_id { get; set; }
        public string Category_Name { get; set; } = string.Empty;
    }
}
