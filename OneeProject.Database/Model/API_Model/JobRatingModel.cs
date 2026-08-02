using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class JobRatingModelForInsert
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        /// <summary>Optional written feedback.</summary>
        public string Feedback { get; set; } = string.Empty;
    }

    public class JobRatingModelForView
    {
        public int Id { get; set; }
        public int FK_job_ID { get; set; }
        public string Problem_Text { get; set; } = string.Empty;
        public string FK_worker_ID { get; set; } = string.Empty;
        public string Worker_Name { get; set; } = string.Empty;
        public string FK_customer_ID { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }
}
