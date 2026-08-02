using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_admin_notifications")]
    public class AdminNotification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>Users | Workers | Both</summary>
        [Required]
        [MaxLength(20)]
        public string Audience { get; set; } = AdminNotificationAudiences.Users;

        /// <summary>Draft | Sent</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = AdminNotificationStatuses.Draft;

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();

        public string? SentBy { get; set; }
        public DateTime? SentOn { get; set; }
    }

    public static class AdminNotificationAudiences
    {
        public const string Users = "Users";
        public const string Workers = "Workers";
        public const string Both = "Both";
    }

    public static class AdminNotificationStatuses
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
    }
}
