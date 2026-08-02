using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_job_chat_messages")]
    public class JobChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int FK_job_ID { get; set; }

        [Required]
        [MaxLength(255)]
        public string FK_sender_ID { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
    }
}
