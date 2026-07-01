using Microsoft.AspNetCore.Identity;
using OneeProject.Database.Common;
using System.ComponentModel.DataAnnotations;

namespace OneeProject.Database.Context
{
    public class AppUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(250)]
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = CommonResources.LocalDatetime();
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool MustChangePassword { get; set; } = false;
    }
}
