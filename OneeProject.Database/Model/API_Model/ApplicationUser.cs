using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OneeProject.Database.Model.API_Model
{
    public class UserRegistration
    {
        public virtual string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string IsActive { get; set; } = string.Empty;
        public string IsOnline { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public virtual IFormFile? Image { get; set; }
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;
    }

    public class LoginUserDetailsModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProImg { get; set; } = string.Empty;
    }

    public class TokenRequestModel
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }
        public bool RememberMe { get; set; } = false;
    }

    public class AuthenticationModel
    {
        public string? Message { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }

    public class AccountDetailsModelForTable
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string IsActive { get; set; } = string.Empty;
        public string IsOnline { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginDate { get; set; }

    }

    public class AccountDetailsModelByUser : UserRegistration
    {
        [JsonIgnore]
        public override IFormFile? Image { get; set; }

        public List<SavedAddressModelForView> Addresses { get; set; } = new();
    }

    public class AccountDetailsModelForUpdate : UserRegistration
    {
        public string LastUpdatedBy { get; set; } = string.Empty;

        public DateTime LastUpdatedOn { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }

    public class UsersForDropdown
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class PaginationView
    {
        public int Count { get; set; }
        public List<AccountDetailsModelForTable> Accounts { get; set; }
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
