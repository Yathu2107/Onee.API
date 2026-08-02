using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class NotificationModelForView
    {
        public int Id { get; set; }
        public string FK_user_ID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? FK_job_ID { get; set; }
        public int? FK_admin_notification_ID { get; set; }
        public bool Is_Read { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class NotificationModelWithPagination
    {
        public int Count { get; set; }
        public List<NotificationModelForView> Notifications { get; set; } = new();
    }

    public class AdminNotificationModelForInsert
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>Users | Workers | Both</summary>
        [Required]
        public string Audience { get; set; } = "Users";
    }

    public class AdminNotificationModelForView
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? SentBy { get; set; }
        public DateTime? SentOn { get; set; }
        public int RecipientCount { get; set; }
    }

    public class AdminNotificationModelWithPagination
    {
        public int Count { get; set; }
        public List<AdminNotificationModelForView> Notifications { get; set; } = new();
    }
}
