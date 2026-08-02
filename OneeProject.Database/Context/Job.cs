using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_jobs")]
    public class Job
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Problem_Text { get; set; } = string.Empty;

        public int Category_id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FK_customer_ID { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FK_worker_ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = JobStatuses.Offering;

        public decimal? Amount { get; set; }

        [MaxLength(500)]
        public string? Cancel_Reason { get; set; }

        public DateTime? Offer_Expires_At { get; set; }

        /// <summary>
        /// JSON array of worker user ids already offered this job.
        /// </summary>
        public string Tried_Worker_Ids { get; set; } = "[]";

        /// <summary>
        /// JSON ordered list of worker ids selected by admin (offer cascade order).
        /// </summary>
        public string Queued_Worker_Ids { get; set; } = "[]";

        public double Customer_Latitude { get; set; }
        public double Customer_Longitude { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public static class JobStatuses
    {
        public const string Offering = "Offering";
        public const string Accepted = "Accepted";
        public const string Ongoing = "Ongoing";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Failed = "Failed";
    }
}
