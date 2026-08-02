using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class ComplaintModelForInsert
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }

    public class ComplaintModelForUpdateStatus
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        public string? Admin_Response { get; set; }
    }

    public class ComplaintModelForView
    {
        public int Id { get; set; }
        public int FK_job_ID { get; set; }
        public string Problem_Text { get; set; } = string.Empty;
        public string FK_customer_ID { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public string FK_worker_ID { get; set; } = string.Empty;
        public string Worker_Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Admin_Response { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public class ComplaintModelWithPagination
    {
        public int Count { get; set; }
        public List<ComplaintModelForView> Complaints { get; set; } = new();
    }
}
