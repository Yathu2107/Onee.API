using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Model.API_Model
{
    public class SavedAddressModelForInsert
    {
        [Required]
        [MaxLength(50)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address_Line { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public bool Is_Default { get; set; }
    }

    public class SavedAddressModelForUpdate
    {
        [Required]
        [MaxLength(50)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address_Line { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public bool Is_Default { get; set; }
    }

    public class SavedAddressModelForView
    {
        public int Id { get; set; }
        public string FK_user_ID { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Address_Line { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Is_Default { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }

    public class SavedAddressModelWithPagination
    {
        public int Count { get; set; }
        public List<SavedAddressModelForView> Addresses { get; set; } = new();
    }
}
