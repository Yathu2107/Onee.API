using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_worker_categories")]
    public class WorkerCategory
    {
        [Required]
        [Column("FK_user_ID")]
        [MaxLength(255)]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required]
        [Column("Category_id")]
        public int Category_id { get; set; }
    }
}
