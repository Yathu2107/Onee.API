using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_complaints")]
    public class Complaint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int FK_job_ID { get; set; }

        [Required]
        [MaxLength(255)]
        public string FK_customer_ID { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FK_worker_ID { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = ComplaintStatuses.Open;

        public string? Admin_Response { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public static class ComplaintStatuses
    {
        public const string Open = "Open";
        public const string InReview = "InReview";
        public const string Resolved = "Resolved";
        public const string Rejected = "Rejected";
    }
}
