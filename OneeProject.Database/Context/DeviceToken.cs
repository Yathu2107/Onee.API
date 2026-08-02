using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneeProject.Database.Common;

namespace OneeProject.Database.Context
{
    [Table("t_device_tokens")]
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required, MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Platform { get; set; } = "android";

        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
        public DateTime LastUpdatedOn { get; set; } = CommonResources.LocalDatetime();
    }
}
