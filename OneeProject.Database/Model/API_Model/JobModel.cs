using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class JobCreateModel
    {
        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Ordered worker ids from admin (find-workers). First gets the initial 1-min offer.
        /// </summary>
        [Required]
        [MinLength(1)]
        public List<string> WorkerIds { get; set; } = new();

        /// <summary>
        /// Optional saved address for the job location. Falls back to Is_Default then user lat/lng.
        /// </summary>
        public int? AddressId { get; set; }
    }

    public class JobConfirmModel
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class JobCancelModel
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class JobChatSendModel
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Who is sending (customer or assigned worker). Admin panel passes the speaker id.
        /// </summary>
        [Required]
        public string SenderId { get; set; } = string.Empty;
    }

    public class JobChatMessageModel
    {
        public int Id { get; set; }
        public int FK_job_ID { get; set; }
        public string FK_sender_ID { get; set; } = string.Empty;
        public string Sender_Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    public class JobModelForDetailView
    {
        public int Id { get; set; }
        public string Problem_Text { get; set; } = string.Empty;
        public int Category_id { get; set; }
        public string Category_Name { get; set; } = string.Empty;
        public string FK_customer_ID { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public string Customer_Image_Url { get; set; } = string.Empty;
        public string? FK_worker_ID { get; set; }
        public string? Worker_Name { get; set; }
        public string? Worker_Image_Url { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Cancel_Reason { get; set; }
        public DateTime? Offer_Expires_At { get; set; }
        public List<string> Queued_Worker_Ids { get; set; } = new();
        public List<string> Tried_Worker_Ids { get; set; } = new();
        public double Customer_Latitude { get; set; }
        public double Customer_Longitude { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<JobChatMessageModel> Messages { get; set; } = new();
        public bool HasRating { get; set; }
        public JobRatingModelForView? Rating { get; set; }
    }

    public class JobModelForTable
    {
        public int Id { get; set; }
        public string Problem_Text { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public string? Worker_Name { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public DateTime? Offer_Expires_At { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class JobModelWithPagination
    {
        public List<JobModelForTable> Jobs { get; set; } = new();
        public int Count { get; set; }
    }
}
