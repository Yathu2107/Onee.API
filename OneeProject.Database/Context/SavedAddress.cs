using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneeProject.Database.Context
{
    [Table("t_saved_addresses")]
    public class SavedAddress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FK_user_ID { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address_Line { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool Is_Default { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = CommonResources.LocalDatetime();
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }
}
