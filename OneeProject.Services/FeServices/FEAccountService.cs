using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Database.Model.FEAPI_Model;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OneeProject.Services.FeServices
{
    public class FEAccountService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config, AppDbContext context, IMemoryCache cache)
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IConfiguration _config = config;
        private readonly AppDbContext _context = context;
        private readonly IMemoryCache _cache = cache;

        // OTP purposes — kept separate so they cannot be cross-used
        private const string PURPOSE_LOGIN = "login";
        private const string PURPOSE_FORGOT = "forgot_password";

        // Private helpers
        private static string GenerateOtp()
            => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        /// <summary>
        /// Cache key is PURPOSE-scoped so login OTPs and forgot-password OTPs
        /// are stored under different keys and cannot be swapped.
        /// </summary>
        private static string OtpKey(string purpose, string normalizedMobile)
            => $"otp:{purpose}:{normalizedMobile}";

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task<string> GenerateJwtTokenAsync(AppUser user, IList<string> roles)
        {
            var role = roles.FirstOrDefault() ?? "User";

            var claims = new List<Claim>
            {
                new Claim("uid",                          user.Id),
                new Claim(JwtRegisteredClaimNames.Email,  user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub,    user.Id),
                new Claim(JwtRegisteredClaimNames.Jti,    Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                          DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                          ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Name,                user.Name ?? string.Empty),
                new Claim(ClaimTypes.Role,                role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Private utility
        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        /// <summary>
        /// Strips country prefix and leading zero so the number is stored/compared
        /// in a consistent 9-digit local format.
        /// </summary>
        public string NormalizeNumber(string number)
        {
            number = number.Trim();
            if (number.StartsWith("+94")) number = number[3..];
            else if (number.StartsWith("94")) number = number[2..];
            if (number.StartsWith("0")) number = number[1..];
            return number;
        }

        /// <summary>
        /// Returns the matching AppUser or null. Client-side evaluation is used
        /// because EF cannot translate the normalization logic to SQL.
        /// </summary>
        private AppUser? FindUserByMobile(string normalizedMobile)
            => _context.Users
                       .AsEnumerable()
                       .FirstOrDefault(u => u.PhoneNumber != null
                                         && NormalizeNumber(u.PhoneNumber) == normalizedMobile);

        /// <summary>
        /// Returns the matching AppUser or null. Client-side evaluation is used
        /// because EF cannot translate the normalization logic to SQL.
        /// </summary>
        private AppUser? FindUserById(string userId)
            => _context.Users
                       .AsEnumerable()
                       .FirstOrDefault(u => u.Id != null
                                         && u.Id == userId);

        /// <summary>
        /// A user is considered "existing / registered" only when their Name
        /// has been filled in. A stub record (created during OTP verification
        /// for new users) has Name == null.
        /// </summary>
        private static bool IsRegisteredUser(AppUser? user)
            => user != null && !string.IsNullOrWhiteSpace(user.Name);

        /// <summary>
        /// Sends an OTP, enforcing a 2-minute cooldown between requests.
        /// Returns (otp, errorMessage). On error, otp is null.
        /// </summary>
        private async Task<(string? Otp, Message<string>? Error)> SendOtpAsync(
            string normalizedMobile,
            string purpose)
        {
            var key = OtpKey(purpose, normalizedMobile);
            var otpInfo = _cache.Get<OtpInfo>(key) ?? new OtpInfo
            {
                IsUsed = true,
                LastRequestedAt = DateTime.MinValue
            };

            var now = DateTime.UtcNow;

            // Enforce 2-minute cooldown if the previous OTP has not been used yet
            if (!otpInfo.IsUsed && (now - otpInfo.LastRequestedAt).TotalMinutes < 1)
            {
                return (null, new Message<string>
                {
                    Status = "E",
                    Text = "Please wait 1 minutes before requesting another OTP.",
                    Code = "OTP_COOLDOWN",
                    Result = string.Empty
                });
            }

            // Decide OTP value
            string env = _config["EnvironmentSetting:Env"]?.ToUpper() ?? "DEV";
            string? defaultSmsNum = _config["EnvironmentSetting:DefaultSMSNumber"];
            bool isDev = env == "DEV";

            string otp = (isDev || (!string.IsNullOrWhiteSpace(defaultSmsNum) && normalizedMobile == defaultSmsNum))
                ? "999999"
                : GenerateOtp();

            otpInfo.Code = otp;
            otpInfo.ExpiresAt = now.AddMinutes(1);
            otpInfo.IsUsed = false;
            otpInfo.LastRequestedAt = now;

            _cache.Set(key, otpInfo, otpInfo.ExpiresAt);

            // Send SMS when not in DEV or when number differs from the default test number
            bool shouldSendSms = !isDev ||
                (!string.IsNullOrWhiteSpace(defaultSmsNum) && normalizedMobile != defaultSmsNum);

            if (shouldSendSms)
            {
                var smsService = new CommunicationService();
                await smsService.SendMessageAsync(
                    normalizedMobile,
                    $"Your OTP is {otp}. It will expire in 1 minute.");
            }

            return (otp, null);
        }


        private (bool Valid, Message<FEAuthenticationModel>? Error) ValidateOtp(
            string normalizedMobile,
            string otp,
            string purpose)
        {
            var key = OtpKey(purpose, normalizedMobile);
            var otpInfo = _cache.Get<OtpInfo>(key);

            if (otpInfo == null || otpInfo.IsUsed || otpInfo.Code != otp || otpInfo.ExpiresAt < DateTime.UtcNow)
            {
                return (false, new Message<FEAuthenticationModel>
                {
                    Status = "E",
                    Text = "OTP expired or invalid.",
                    Code = "OTP_INVALID",
                    Result = null
                });
            }

            // Consume the OTP immediately so it cannot be replayed
            otpInfo.IsUsed = true;
            _cache.Set(key, otpInfo, otpInfo.ExpiresAt);

            return (true, null);
        }

        // Public service methods

        public async Task<Message<string>> VerifyMobileAsync(string mobile)
        {
            string normalizedMobile = NormalizeNumber(mobile);
            var user = FindUserByMobile(normalizedMobile);
            bool isExisting = IsRegisteredUser(user);

            // BLOCKED USER CHECK (IMPORTANT)
            if (isExisting && user != null && user.IsActive == false)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Your account has been blocked. Please contact support.",
                    Code = "ACCOUNT_BLOCKED",
                    Result = string.Empty
                };
            }

            // All other cases → send OTP
            var (_, error) = await SendOtpAsync(normalizedMobile, PURPOSE_LOGIN);
            if (error != null)
                return error;

            // Determine response code and message
            string code, text;

            // APP — always OTP
            code = "OTP_SENT";
            text = "OTP sent successfully.";

            return new Message<string>
            {
                Status = "S",
                Text = text,
                Code = code,
                Result = string.Empty
            };
        }

        public async Task<Message<FEAuthenticationModel>> VerifyOtpAndProceedAsync(
            string mobile,
            string otp)
        {
            string normalizedMobile = NormalizeNumber(mobile);

            var (valid, otpError) = ValidateOtp(normalizedMobile, otp, PURPOSE_LOGIN);
            if (!valid)
                return otpError!;

            var user = FindUserByMobile(normalizedMobile);
            bool isNew = !IsRegisteredUser(user);

            // Create stub record for new users so they can be authorized for registration
            if (isNew && user == null)
            {
                user = new AppUser
                {
                    UserName = normalizedMobile,
                    PhoneNumber = normalizedMobile,
                    Name = string.Empty,          // Name == null marks this as a stub / unregistered
                    UserType = "Customer",
                    IsActive = true,
                    MustChangePassword = false
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new Message<FEAuthenticationModel>
                    {
                        Status = "E",
                        Text = "Failed to initialise user record: " +
                                 string.Join("; ", createResult.Errors.Select(e => e.Description)),
                        Code = "STUB_CREATE_FAILED",
                        Result = null
                    };
                }
            }

            // Determine next step
            string nextStep;
            if (isNew)
            {
                nextStep = "register";
            }
            else
            {
                nextStep = "home_page";
            }

            var roles = await _userManager.GetRolesAsync(user!);
            var jwt = await GenerateJwtTokenAsync(user!, roles);

            return new Message<FEAuthenticationModel>
            {
                Status = "S",
                Text = "OTP verified successfully.",
                Code = "OTP_VERIFIED",
                Result = new FEAuthenticationModel
                {
                    UserName = user!.Name,
                    Email = user.Email,
                    Role = string.Join(",", roles),
                    Token = jwt,
                    RefreshToken = GenerateRefreshToken(),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(10),
                    ForcePasswordReset = !isNew && string.IsNullOrWhiteSpace(user.PasswordHash),
                    NextStep = nextStep
                }
            };
        }


        public async Task<(bool Success, string Message)> RegisterUserAsync( InsertUser u, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if email already exists
                if (!string.IsNullOrWhiteSpace(u.Email))
                {
                    var existingEmailUser = await _userManager.FindByEmailAsync(u.Email);

                    if (existingEmailUser != null && existingEmailUser.Id != userId)
                    {
                        return (false, "This email address is already registered.");
                    }
                }

                // Handle profile image upload
                if (u.Image != null)
                {
                    string[] imageProperties = { "ProfileImageUrl" };
                    IFormFile[] images = { u.Image };

                    u = FeSaveFiles.SetImageUrl(u, images, imageProperties, "User");
                }

                var existingUser = FindUserById(userId);

                if (existingUser != null)
                {
                    // Update existing stub user
                    existingUser.UserName = string.IsNullOrWhiteSpace(u.Email)
                        ? u.PhoneNumber
                        : u.Email;

                    existingUser.Email = u.Email;
                    existingUser.Name = u.Name;

                    existingUser.ProfileImageUrl =
                        string.IsNullOrWhiteSpace(u.ProfileImageUrl)
                            ? "Default.png"
                            : u.ProfileImageUrl;

                    existingUser.IsActive = true;
                    existingUser.MustChangePassword = false;

                    var updateResult = await _userManager.UpdateAsync(existingUser);

                    if (!updateResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

                        return (false, $"Registration update failed: {errors}");
                    }

                    // Assign role
                    await EnsureRoleAsync(existingUser.UserType ?? "User");

                    if (!await _userManager.IsInRoleAsync(existingUser, existingUser.UserType ?? "User"))
                    {
                        await _userManager.AddToRoleAsync(
                            existingUser,
                            existingUser.UserType ?? "User");
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return (true, "User registered successfully.");
                }
                else
                {
                    return (false, "User registration failed");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return (false, $"Transaction failed: {ex.Message}");
            }
        }

        public async Task<Message<string>> UpdateUserAsync(FEUpdateUserRequest model, string userId)
        {
            var user = FindUserById(userId);

            if (user == null)
                return new Message<string>
                {
                    Status = "E",
                    Text = "User not found.",
                    Code = "USER_NOT_FOUND"
                };

            // Check if email is already taken by a DIFFERENT user
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailTaken = _context.Users
                    .Any(u => u.Email == model.Email && u.Id != user.Id);

                if (emailTaken)
                    return new Message<string>
                    {
                        Status = "E",
                        Text = "This email address is already in use by another account.",
                        Code = "EMAIL_ALREADY_EXISTS"
                    };
            }

            // Update basic profile fields
            user.Name = model.Name;
            user.Email = model.Email;
            user.LastUpdatedBy = userId;
            user.LastUpdatedOn = CommonResources.LocalDatetime();

            // Image upload
            if (model.Image != null)
            {
                string[] imageProperties = { "ProfileImageUrl" };
                IFormFile[] images = { model.Image };

                var tempUser = new UserRegistration
                {
                    ProfileImageUrl = user.ProfileImageUrl,
                    Image = model.Image
                };

                tempUser = FeSaveFiles.SetImageUrl(tempUser, images, imageProperties, "User");
                user.ProfileImageUrl = tempUser.ProfileImageUrl;
            }

            await _userManager.UpdateAsync(user);

            return new Message<string>
            {
                Status = "S",
                Text = "User details updated successfully.",
                Code = "USER_UPDATED",
                Result = user.Id
            };
        }

        public async Task<FELoggedUserDetailsModel?> GetUserDetailsAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var uploadPath = _config["EnvironmentSetting:UploadPath"] ?? "";

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Name,
                    u.Email,
                    u.ProfileImageUrl,
                    u.PhoneNumber,
                    u.IsActive
                })
                .SingleOrDefaultAsync();

            if (user == null)
                return null;

            return new FELoggedUserDetailsModel
            {
                Name = user.Name,
                Email = user.Email,
                ProImg = $"{uploadPath}/User/{user.ProfileImageUrl}",
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive
            };
        }

        public async Task<Message<string>> ResendOtpAsync(string mobile, string purpose)
        {
            string normalizedMobile = NormalizeNumber(mobile);

            string otpPurpose = purpose.ToLower() switch
            {
                "login" => PURPOSE_LOGIN,
                "forgot_password" => PURPOSE_FORGOT,
                _ => PURPOSE_LOGIN
            };

            var (_, error) = await SendOtpAsync(normalizedMobile, otpPurpose);

            if (error != null)
                return error;

            return new Message<string>
            {
                Status = "S",
                Text = "OTP resent successfully.",
                Code = "OTP_RESENT",
                Result = string.Empty
            };
        }
    }
}
