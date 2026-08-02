using Microsoft.AspNetCore.Http;

namespace OneeProject.Database.Model.FEAPI_Model.User
{
    public class FEApplicationUser
    {
        public virtual string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string IsActive { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public virtual IFormFile? Image { get; set; }
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;
    }

    public class InsertUser : FEApplicationUser { }

    public class FEUpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }

    public class FELoggedUserDetailsModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProImg { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;
    }

    public class OtpInfo
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime LastRequestedAt { get; set; }
        public bool IsUsed { get; set; }
    }

    public class FEVerifyOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class FELocationUpdateModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class FEOnlineStatusModel
    {
        public bool IsOnline { get; set; }
    }

    public class FEAuthenticationModel
    {
        public string? Message { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public string? NextStep { get; set; } = string.Empty;
    }
}
