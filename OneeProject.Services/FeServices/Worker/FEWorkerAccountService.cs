using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Database.Model.FEAPI_Model.Worker;
using OneeProject.Services.Helper;
using OneeProject.Services.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OneeProject.Services.FeServices.Worker
{
    public class FEWorkerAccountService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        AppDbContext context,
        IMemoryCache cache,
        CommunicationService communicationService,
        AddressService addressService)
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IConfiguration _config = config;
        private readonly AppDbContext _context = context;
        private readonly IMemoryCache _cache = cache;
        private readonly CommunicationService _communicationService = communicationService;
        private readonly AddressService _addressService = addressService;

        private const string AppRole = "Worker";
        private const int OtpExpiryMinutes = 5;
        private const int OtpCooldownMinutes = 1;

        private bool IsDevEnv =>
            string.Equals(_config["EnvironmentSetting:Env"], "DEV", StringComparison.OrdinalIgnoreCase);

        private string DevDefaultOtp =>
            _config["EnvironmentSetting:DefaultOtp"] ?? "999999";

        private static string GenerateOtp()
            => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        private static string OtpKey(string normalizedPhone)
            => $"otp:worker:{normalizedPhone}";

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        /// <summary>Local DB format: 07xxxxxxxx</summary>
        private static string NormalizePhone(string phone)
            => CommunicationService.NormalizePhoneForDb(phone);

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task<string> GenerateJwtTokenAsync(AppUser user, IList<string> roles)
        {
            var role = roles.FirstOrDefault() ?? AppRole;

            var claims = new List<Claim>
            {
                new("uid", user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new(ClaimTypes.Name, user.Name ?? string.Empty),
                new(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        private AppUser? FindUserById(string userId)
            => _context.Users.FirstOrDefault(u => u.Id == userId);

        private static bool IsRegisteredUser(AppUser? user)
            => user != null && !string.IsNullOrWhiteSpace(user.Name);

        private Message<string>? RejectWrongApp(AppUser user)
        {
            if (string.Equals(user.UserType, "User", StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.UserType, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "This account is a User. Please use the User app.",
                    Code = "WRONG_APP",
                    Result = string.Empty
                };
            }

            if (string.Equals(user.UserType, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Admin accounts cannot sign in to the Worker app.",
                    Code = "WRONG_APP",
                    Result = string.Empty
                };
            }

            return null;
        }

        private AppUser? FindUserByPhone(string phone)
        {
            var local = NormalizePhone(phone);
            var intl = CommunicationService.NormalizeToTextLkRecipient(phone);

            return _context.Users.FirstOrDefault(u =>
                u.PhoneNumber == local
                || u.PhoneNumber == intl
                || u.PhoneNumber == phone.Trim());
        }

        private async Task<(string? Otp, Message<string>? Error)> SendOtpAsync(string normalizedPhone)
        {
            var key = OtpKey(normalizedPhone);
            var otpInfo = _cache.Get<FEWorkerOtpInfo>(key) ?? new FEWorkerOtpInfo
            {
                IsUsed = true,
                LastRequestedAt = DateTime.MinValue
            };

            var now = DateTime.UtcNow;

            if (!otpInfo.IsUsed && (now - otpInfo.LastRequestedAt).TotalMinutes < OtpCooldownMinutes)
            {
                return (null, new Message<string>
                {
                    Status = "E",
                    Text = "Please wait 1 minute before requesting another OTP.",
                    Code = "OTP_COOLDOWN",
                    Result = string.Empty
                });
            }

            string otp = IsDevEnv ? DevDefaultOtp : GenerateOtp();

            otpInfo.Code = otp;
            otpInfo.ExpiresAt = now.AddMinutes(OtpExpiryMinutes);
            otpInfo.IsUsed = false;
            otpInfo.LastRequestedAt = now;
            _cache.Set(key, otpInfo, otpInfo.ExpiresAt);

            // Skip SMS when EnvironmentSetting:Env = DEV
            if (!IsDevEnv)
            {
                var smsText =
                    $"Welcome to Onee! Your worker verification code is {otp}. This code expires in {OtpExpiryMinutes} minutes. Do not share it with anyone.";

                var (sent, smsMessage) = await _communicationService.SendMessageAsync(normalizedPhone, smsText);
                if (!sent)
                {
                    return (null, new Message<string>
                    {
                        Status = "E",
                        Text = smsMessage,
                        Code = "OTP_SEND_FAILED",
                        Result = string.Empty
                    });
                }
            }

            return (otp, null);
        }

        private (bool Valid, Message<FEWorkerAuthenticationModel>? Error) ValidateOtp(
            string normalizedPhone,
            string otp)
        {
            var key = OtpKey(normalizedPhone);
            var otpInfo = _cache.Get<FEWorkerOtpInfo>(key);

            if (otpInfo == null || otpInfo.IsUsed || otpInfo.Code != otp || otpInfo.ExpiresAt < DateTime.UtcNow)
            {
                return (false, new Message<FEWorkerAuthenticationModel>
                {
                    Status = "E",
                    Text = "OTP expired or invalid.",
                    Code = "OTP_INVALID",
                    Result = null
                });
            }

            otpInfo.IsUsed = true;
            _cache.Set(key, otpInfo, otpInfo.ExpiresAt);
            return (true, null);
        }

        public async Task<Message<string>> VerifyPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "A valid phone number is required.",
                    Code = "INVALID_PHONE",
                    Result = string.Empty
                };
            }

            var normalizedPhone = NormalizePhone(phone);
            if (normalizedPhone.Length < 10)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "A valid phone number is required.",
                    Code = "INVALID_PHONE",
                    Result = string.Empty
                };
            }

            var user = FindUserByPhone(normalizedPhone);

            if (user != null)
            {
                var wrongApp = RejectWrongApp(user);
                if (wrongApp != null) return wrongApp;

                if (IsRegisteredUser(user) && !user.IsActive)
                {
                    return new Message<string>
                    {
                        Status = "E",
                        Text = "Your account has been blocked. Please contact support.",
                        Code = "ACCOUNT_BLOCKED",
                        Result = string.Empty
                    };
                }
            }

            var (_, error) = await SendOtpAsync(normalizedPhone);
            if (error != null) return error;

            return new Message<string>
            {
                Status = "S",
                Text = IsDevEnv
                    ? $"OTP ready (DEV). Use {DevDefaultOtp}."
                    : "OTP sent successfully.",
                Code = "OTP_SENT",
                Result = string.Empty
            };
        }

        public async Task<Message<FEWorkerAuthenticationModel>> VerifyOtpAndProceedAsync(
            string phone,
            string otp)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return new Message<FEWorkerAuthenticationModel>
                {
                    Status = "E",
                    Text = "Phone number is required.",
                    Code = "INVALID_PHONE",
                    Result = null
                };
            }

            var normalizedPhone = NormalizePhone(phone);
            var (valid, otpError) = ValidateOtp(normalizedPhone, otp);
            if (!valid) return otpError!;

            var user = FindUserByPhone(normalizedPhone);

            if (user != null)
            {
                var wrongApp = RejectWrongApp(user);
                if (wrongApp != null)
                {
                    return new Message<FEWorkerAuthenticationModel>
                    {
                        Status = "E",
                        Text = wrongApp.Text,
                        Code = wrongApp.Code,
                        Result = null
                    };
                }
            }

            bool isNew = !IsRegisteredUser(user);

            if (isNew && user == null)
            {
                user = new AppUser
                {
                    UserName = normalizedPhone,
                    PhoneNumber = normalizedPhone,
                    Email = null,
                    Name = string.Empty,
                    UserType = AppRole,
                    IsActive = true,
                    IsOnline = false
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new Message<FEWorkerAuthenticationModel>
                    {
                        Status = "E",
                        Text = "Failed to initialise worker record: " +
                               string.Join("; ", createResult.Errors.Select(e => e.Description)),
                        Code = "STUB_CREATE_FAILED",
                        Result = null
                    };
                }

                await EnsureRoleAsync(AppRole);
                await _userManager.AddToRoleAsync(user, AppRole);
            }
            else if (user != null && string.IsNullOrWhiteSpace(user.UserType))
            {
                user.UserType = AppRole;
                await _userManager.UpdateAsync(user);
            }

            string nextStep = isNew ? "register" : "home_page";

            if (!isNew && user != null)
            {
                // Worker stays offline until they flip the availability toggle.
                user.PhoneNumber = normalizedPhone;
                user.IsOnline = false;
                user.LastLoginDate = CommonResources.LocalDatetime();
                await _userManager.UpdateAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user!);
            var jwt = await GenerateJwtTokenAsync(user!, roles);

            return new Message<FEWorkerAuthenticationModel>
            {
                Status = "S",
                Text = "OTP verified successfully.",
                Code = "OTP_VERIFIED",
                Result = new FEWorkerAuthenticationModel
                {
                    UserName = user!.Name,
                    Email = user.Email,
                    Role = string.Join(",", roles),
                    Token = jwt,
                    RefreshToken = GenerateRefreshToken(),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(10),
                    NextStep = nextStep
                }
            };
        }

        public async Task<(bool Success, string Message)> RegisterWorkerAsync(InsertWorker u, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(u.Email))
                {
                    var existingEmailUser = await _userManager.FindByEmailAsync(NormalizeEmail(u.Email));
                    if (existingEmailUser != null && existingEmailUser.Id != userId)
                        return (false, "This email address is already registered.");
                }

                if (u.Image != null)
                {
                    string[] imageProperties = { "ProfileImageUrl" };
                    IFormFile[] images = { u.Image };
                    u = FeSaveFiles.SetImageUrl(u, images, imageProperties, "Worker");
                }

                var existingUser = FindUserById(userId);
                if (existingUser == null)
                    return (false, "Worker registration failed");

                if (string.IsNullOrWhiteSpace(u.Email))
                    return (false, "Email is required for registration.");

                var normalizedEmail = NormalizeEmail(u.Email);
                existingUser.Email = normalizedEmail;
                existingUser.UserName = normalizedEmail;
                existingUser.NormalizedUserName = normalizedEmail.ToUpperInvariant();
                existingUser.NormalizedEmail = normalizedEmail.ToUpperInvariant();
                existingUser.Name = u.Name;
                existingUser.PhoneNumber = string.IsNullOrWhiteSpace(u.PhoneNumber)
                    ? existingUser.PhoneNumber
                    : NormalizePhone(u.PhoneNumber);
                // Location stored in SavedAddress only — do not write t_user lat/lng
                existingUser.ProfileImageUrl = string.IsNullOrWhiteSpace(u.ProfileImageUrl)
                    ? "Default.png"
                    : u.ProfileImageUrl;
                existingUser.UserType = AppRole;
                existingUser.IsActive = true;
                existingUser.IsOnline = false;
                existingUser.LastLoginDate = CommonResources.LocalDatetime();

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    return (false, $"Registration update failed: {errors}");
                }

                await EnsureRoleAsync(AppRole);
                if (!await _userManager.IsInRoleAsync(existingUser, AppRole))
                    await _userManager.AddToRoleAsync(existingUser, AppRole);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Worker registered successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Transaction failed: {ex.Message}");
            }
        }

        public async Task<Message<string>> UpdateWorkerAsync(FEUpdateWorkerRequest model, string userId)
        {
            var user = FindUserById(userId);
            if (user == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "USER_NOT_FOUND"
                };
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var normalizedEmail = NormalizeEmail(model.Email);
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.Email == normalizedEmail && u.Id != user.Id);

                if (emailTaken)
                {
                    return new Message<string>
                    {
                        Status = "E",
                        Text = "This email address is already in use by another account.",
                        Code = "EMAIL_ALREADY_EXISTS"
                    };
                }

                user.Email = normalizedEmail;
                user.UserName = normalizedEmail;
                user.NormalizedUserName = normalizedEmail.ToUpperInvariant();
                user.NormalizedEmail = normalizedEmail.ToUpperInvariant();
            }

            user.Name = model.Name;
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                user.PhoneNumber = NormalizePhone(model.PhoneNumber);
            user.LastUpdatedBy = userId;
            user.LastUpdatedOn = CommonResources.LocalDatetime();

            if (model.Image != null)
            {
                string[] imageProperties = { "ProfileImageUrl" };
                IFormFile[] images = { model.Image };
                var tempUser = new UserRegistration
                {
                    ProfileImageUrl = user.ProfileImageUrl,
                    Image = model.Image
                };
                tempUser = FeSaveFiles.SetImageUrl(tempUser, images, imageProperties, "Worker");
                user.ProfileImageUrl = tempUser.ProfileImageUrl;
            }

            await _userManager.UpdateAsync(user);

            return new Message<string>
            {
                Status = "S",
                Text = "Worker details updated successfully.",
                Code = "USER_UPDATED",
                Result = user.Id
            };
        }

        public async Task<Message<string>> UpdateLocationAsync(
            FEWorkerLocationUpdateModel model,
            string userId)
        {
            var user = FindUserById(userId);
            if (user == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "USER_NOT_FOUND"
                };
            }

            return await _addressService.SetLocationAsDefaultAsync(
                userId,
                model.Latitude,
                model.Longitude,
                userId);
        }

        public async Task<Message<string>> SetOnlineStatusAsync(string userId, bool isOnline)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "404",
                    Result = null
                };
            }

            user.IsOnline = isOnline;
            user.LastUpdatedBy = userId;
            user.LastUpdatedOn = CommonResources.LocalDatetime();
            await _userManager.UpdateAsync(user);

            return new Message<string>
            {
                Status = "S",
                Text = isOnline ? "Worker is now online." : "Worker is now offline.",
                Code = "ONLINE_STATUS_UPDATED",
                Result = isOnline ? "Online" : "Offline"
            };
        }

        public async Task<Message<string>> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found.",
                    Code = "404",
                    Result = null
                };
            }

            user.IsOnline = false;
            user.LastUpdatedBy = userId;
            user.LastUpdatedOn = CommonResources.LocalDatetime();
            await _userManager.UpdateAsync(user);

            return new Message<string>
            {
                Status = "S",
                Text = "Logged out successfully.",
                Code = "200",
                Result = string.Empty
            };
        }

        public async Task<FELoggedWorkerDetailsModel?> GetWorkerDetailsAsync(string id)
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
                    u.IsActive,
                    u.IsOnline
                })
                .SingleOrDefaultAsync();

            if (user == null)
                return null;

            var defaultAddress = await _context.SavedAddresses
                .Where(a => a.FK_user_ID == id && a.Is_Default)
                .Select(a => new { a.Latitude, a.Longitude })
                .FirstOrDefaultAsync();

            var ratingStats = await _context.JobRatings
                .Where(r => r.FK_worker_ID == id)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .FirstOrDefaultAsync();

            return new FELoggedWorkerDetailsModel
            {
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                ProImg = CommonResources.BuildUploadUrl(uploadPath, "Worker", user.ProfileImageUrl),
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                IsOnline = user.IsOnline,
                Latitude = defaultAddress?.Latitude ?? 0,
                Longitude = defaultAddress?.Longitude ?? 0,
                AverageRating = ratingStats == null
                    ? 0
                    : Math.Round(ratingStats.Average, 1),
                RatingCount = ratingStats?.Count ?? 0
            };
        }

        public async Task<Message<string>> ResendOtpAsync(string phone)
            => await VerifyPhoneAsync(phone);
    }
}
