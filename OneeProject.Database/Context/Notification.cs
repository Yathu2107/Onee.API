using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = NotificationTypes.System;

        public int? FK_job_ID { get; set; }

        public int? FK_admin_notification_ID { get; set; }

        public bool Is_Read { get; set; }

        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
    }

    public static class NotificationTypes
    {
        public const string JobOffer = "job_offer";
        public const string JobUpdated = "job_updated";
        public const string ChatMessage = "chat_message";
        public const string ComplaintUpdate = "complaint_update";
        public const string AdminBroadcast = "admin_broadcast";
        public const string System = "system";
    }
}
