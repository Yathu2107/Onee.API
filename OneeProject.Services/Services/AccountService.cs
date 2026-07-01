using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Helper;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OneeProject.Services.Services
{
    public class AccountService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config, AppDbContext context, IMemoryCache cache)
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IConfiguration _config = config;
        private readonly AppDbContext _context = context;
        private readonly IMemoryCache _cache = cache;
        private const int OTP_EXPIRY_MINUTES = 5;


        public async Task<(bool Success, string Message)> RegisterAdminAsync(UserRegistration u)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Handle image
                if (u.Image != null)
                {
                    string[] imageProperties = { "ProfileImageUrl" };
                    IFormFile[] images = { u.Image };

                    u = SaveFiles.SetImageUrl(u, images, imageProperties, "User");
                }

                // Decide password
                string password;
                bool isAutoPassword = u.PasswordMode?.ToUpper() == "AUTO";

                if (isAutoPassword)
                {
                    password = GenerateStrongPassword();
                }
                else
                {
                    password = u.Password;

                    if (string.IsNullOrWhiteSpace(password))
                        return (false, "Password is required.");
                }

                // Create user
                var user = new AppUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    Name = u.Name,
                    PhoneNumber = u.PhoneNumber,
                    ProfileImageUrl = string.IsNullOrWhiteSpace(u.ProfileImageUrl)
                        ? "Default.png"
                        : u.ProfileImageUrl,

                    UserType = u.UserType,

                    IsActive = true,
                    MustChangePassword = isAutoPassword
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();

                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));

                    return (false, $"User creation failed: {errors}");
                }

                // Disable lockout
                await _userManager.SetLockoutEnabledAsync(user, false);

                // Create role if not exists
                if (!await _roleManager.RoleExistsAsync(u.UserType))
                {
                    await _roleManager.CreateAsync(new IdentityRole(u.UserType));
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, u.UserType);

                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                // Send email after successful commit
                if (isAutoPassword)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var htmlBody = EmailTemplateHelper.LoadAndFormat(
                                "AutoPasswordTemplate.html",
                                new Dictionary<string, string>
                                {
                            { "PASSWORD", password }
                                });

                            var emailService = new EmailService(_config);

                            await emailService.SendEmailAsync(
                                user.Email,
                                "Your MPMart Account Password",
                                htmlBody
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("EMAIL FAILED: " + ex.Message);
                        }
                    });
                }

                return (
                    true,
                    isAutoPassword
                        ? "User registered successfully. Password sent via email."
                        : "User registered successfully."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return (false, $"Transaction failed: {ex.Message}");
            }
        }

        private string GenerateStrongPassword()
        {
            return $"Mp@{Guid.NewGuid():N}".Substring(0, 12) + "A1!";
        }

        public async Task<AuthenticationModel?> LoginAsync(TokenRequestModel model)
        {
            // Always normalize email
            var email = model.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return generic message (prevents account enumeration)
                return Failed("Invalid email or password.", email);
            }

            // Check if account is locked by system admin
            if (await _userManager.IsLockedOutAsync(user))
            {
                return Failed("Your account is locked. Please contact the administrator.", user.Email, user.Name);
            }

            // Validate password
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                // Log failed attempt
                await _userManager.AccessFailedAsync(user);

                // Identity automatically handles lockout threshold.
                // You should configure it in Startup: Lockout.MaxFailedAccessAttempts = 3

                return Failed("Invalid email or password.", user.Email, user.Name);
            }

            // Reset failed attempts
            await _userManager.ResetAccessFailedCountAsync(user);

            // 🔥 FORCE PASSWORD RESET (AUTO PASSWORD FLOW)
            if (user.MustChangePassword)
            {
                return new AuthenticationModel
                {
                    Message = "Password reset required.",
                    Email = user.Email,
                    UserName = user.Name,
                    ForcePasswordReset = true,

                    // 🔒 Do NOT issue tokens
                    Token = null,
                    RefreshToken = null
                };
            }

            // Fetch roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate tokens
            var jwtToken = await GenerateJwtTokenAsync(user, roles);
            var refreshToken = GenerateRefreshToken();

            // Last login auditing
            user.LastLoginDate = CommonResources.LocalDatetime();
            await _userManager.UpdateAsync(user);

            return new AuthenticationModel
            {
                Message = "Login successful.",
                UserName = user.Name,
                Email = user.Email,
                Role = string.Join(",", roles),
                Token = jwtToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiration = model.RememberMe
                    ? DateTime.UtcNow.AddDays(7)
                    : DateTime.UtcNow.AddHours(1),
                ForcePasswordReset = false
            };
        }

        // Common “Failure Response” Helper
        private AuthenticationModel Failed(
        string message,
        string email,
        string? username = null,
        bool forcePasswordReset = false)
        {
            return new AuthenticationModel
            {
                Message = message,
                Email = email,
                UserName = username,
                ForcePasswordReset = forcePasswordReset,
                Token = null,
                RefreshToken = null
            };
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64]; // 512-bit strong token
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }


        private async Task<string> GenerateJwtTokenAsync(AppUser user, IList<string> roles)
        {
            var role = roles.FirstOrDefault() ?? "User";

            var claims = new List<Claim>
            {
                new Claim("uid", user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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

        public async Task<LoginUserDetailsModel?> GetUserDetailsAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new LoginUserDetailsModel
                {
                    Name = u.Name,
                    Email = u.Email,
                    ProImg = u.ProfileImageUrl
                })
                .SingleOrDefaultAsync();

            return user;
        }

        public async Task<PaginationView> GetAllAccountsAsync(
            string id,
            int pageNumber,
            int pageSize,
            string search,
            string status)
        {
            var query = _context.Users.AsQueryable();

            switch (id)
            {
                case "1":
                    query = query.Where(u => u.UserType == "User");
                    break;

                case "2":
                    query = query.Where(u => u.UserType == "Admin");
                    break;

                case "3":
                    query = query.Where(u => u.UserType == "Worker");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.PhoneNumber.Contains(search)
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();
                if (status == "active") query = query.Where(u => u.IsActive);
                else if (status == "blocked") query = query.Where(u => !u.IsActive);
            }

            //var totalRecords = await query.CountAsync();

            var accounts = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AccountDetailsModelForTable
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsActive = u.IsActive ? "Active" : "Blocked",
                    CreatedAt = u.CreatedAt,
                    LastLoginDate = u.LastLoginDate
                })
                .ToListAsync();

            return new PaginationView
            {
                Count = accounts.Count,
                Accounts = accounts
            };
        }

        public async Task<AccountDetailsModelByUser> GetAccountDetailsById(string userId)
        {
            var data = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new AccountDetailsModelByUser
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsActive = u.IsActive ? "Active" : "Blocked",
                })
                .SingleOrDefaultAsync();

            return data;
        }

        public AppUser GetById(string id)
        {
            return _context.Users.Find(id);
        }


        public async Task<Message<string>> UpdateAccountDetailsAsync(
        AccountDetailsModelForUpdate model,
        string userId)
        {
            var existUser = GetById(userId);
            if (existUser == null)
            {
                return new Message<string>
                {
                    Status = "E",
                    Text = "User not found"
                };
            }

            var roles = await _userManager.GetRolesAsync(existUser);

            model.ProfileImageUrl = existUser.ProfileImageUrl;

            // Handle image
            if (model.Image != null)
            {
                string[] imageProperties = { "ProfileImageUrl" };
                IFormFile[] images = { model.Image };
                model = SaveFiles.SetImageUrl(model, images, imageProperties, "User");
            }

            // Update basic info
            existUser.Name = model.Name;
            existUser.Email = model.Email;
            existUser.PhoneNumber = model.PhoneNumber;
            existUser.UserType = model.UserType;
            existUser.LastUpdatedBy = model.LastUpdatedBy;
            existUser.LastUpdatedOn = model.LastUpdatedOn;

            // Update image ONLY if a new image was uploaded
            if (!string.IsNullOrEmpty(model.ProfileImageUrl))
            {
                existUser.ProfileImageUrl = model.ProfileImageUrl;
            }

            // Active / Block
            if (!string.IsNullOrEmpty(model.IsActive))
            {
                bool isActive = model.IsActive.Trim().ToLower() == "true";
                existUser.IsActive = isActive;

                if (isActive)
                {
                    existUser.LockoutEnabled = false;
                    existUser.LockoutEnd = null;
                }
                else
                {
                    existUser.LockoutEnabled = true;
                    existUser.LockoutEnd = DateTimeOffset.MaxValue;
                }
            }

            // 🔥 PASSWORD LOGIC (MANUAL / AUTO)
            string? newPassword = null;
            bool isAutoPassword = model.PasswordMode?.ToUpper() == "AUTO";

            if (isAutoPassword)
            {
                newPassword = GenerateStrongPassword();
                existUser.MustChangePassword = true;
            }
            else if (
                model.PasswordMode?.ToUpper() == "MANUAL" &&
                !string.IsNullOrWhiteSpace(model.Password)
            )
            {
                newPassword = model.Password;
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existUser);
                var passwordResult = await _userManager.ResetPasswordAsync(
                    existUser,
                    resetToken,
                    newPassword
                );

                if (!passwordResult.Succeeded)
                {
                    return new Message<string>
                    {
                        Status = "E",
                        Text = string.Join("; ", passwordResult.Errors.Select(e => e.Description))
                    };
                }
            }

            // Role update
            if (roles.Count > 0 && model.UserType != roles.First())
            {
                foreach (var role in roles)
                    await _userManager.RemoveFromRoleAsync(existUser, role);

                await _userManager.AddToRoleAsync(existUser, model.UserType);
            }
            else if (!roles.Any())
            {
                await _userManager.AddToRoleAsync(existUser, model.UserType);
            }

            await _userManager.UpdateAsync(existUser);

            // 🔥 Send auto password email
            if (isAutoPassword && newPassword != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var htmlBody = EmailTemplateHelper.LoadAndFormat(
                            "AutoPasswordTemplate.html",
                            new Dictionary<string, string>
                            {
                    { "PASSWORD", newPassword }
                            });

                        var emailService = new EmailService(_config);
                        await emailService.SendEmailAsync(
                            existUser.Email,
                            "Your MPMart Account Password Updated",
                            htmlBody
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log the error instead of blocking
                        Console.WriteLine("EMAIL FAILED: " + ex.Message);
                    }
                });
            }

            return new Message<string>
            {
                Status = "S",
                Text = isAutoPassword
                    ? "User updated successfully. New password sent via email."
                    : "User updated successfully.",
                Result = existUser.Id
            };
        }

        private static string GenerateOtp()
            => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        private static string OtpCacheKey(string email)
            => $"otp:{email}";

        private static string OtpVerifiedKey(string email)
            => $"otp-verified:{email}";


        public async Task<Message<string>> SendForgotPasswordOtpAsync(string email)
        {
            email = email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Message<string> { Status = "F", Text = "User not found" };

            var otp = GenerateOtp();

            _cache.Set(
                OtpCacheKey(email),
                otp,
                TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES)
            );

            var htmlBody = EmailTemplateHelper.LoadAndFormat(
                "OtpTemplate.html",
                new Dictionary<string, string> { { "OTP", otp } }
            );

            var emailService = new EmailService(_config);
            await emailService.SendEmailAsync(
                email,
                "MPMart Password Reset OTP",
                htmlBody
            );

            return new Message<string>
            {
                Status = "S",
                Text = "OTP sent to registered email."
            };
        }


        public async Task<Message<string>> VerifyOtpAsync(string email, string otp)
        {
            email = email.Trim().ToLower();

            if (!_cache.TryGetValue(OtpCacheKey(email), out string cachedOtp))
            {
                return new Message<string>
                {
                    Status = "F",
                    Text = "OTP expired. Please request a new one."
                };
            }

            if (cachedOtp != otp)
            {
                return new Message<string>
                {
                    Status = "F",
                    Text = "Invalid OTP."
                };
            }

            _cache.Remove(OtpCacheKey(email));

            _cache.Set(
                OtpVerifiedKey(email),
                true,
                TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES)
            );

            return new Message<string>
            {
                Status = "S",
                Text = "OTP verified successfully. You can now reset your password."
            };
        }


        public async Task<Message<string>> ResetPasswordAsync(string email, string newPassword)
        {
            email = email.Trim().ToLower();

            if (!_cache.TryGetValue(OtpVerifiedKey(email), out bool verified) || !verified)
            {
                return new Message<string>
                {
                    Status = "F",
                    Text = "OTP not verified. Please verify OTP first."
                };
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Message<string> { Status = "F", Text = "Invalid request." };

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return new Message<string>
                {
                    Status = "F",
                    Text = string.Join("; ", result.Errors.Select(e => e.Description))
                };
            }

            _cache.Remove(OtpVerifiedKey(email));

            return new Message<string>
            {
                Status = "S",
                Text = "Password reset successfully."
            };
        }


        public async Task<Message<string>> ResetAutoGeneratedPasswordAsync(string email, string newPassword)
        {
            email = email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Message<string> { Status = "F", Text = "Invalid request." };

            if (!user.MustChangePassword)
                return new Message<string>
                {
                    Status = "F",
                    Text = "Password reset not required for this account."
                };

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return new Message<string>
                {
                    Status = "F",
                    Text = string.Join("; ", result.Errors.Select(e => e.Description))
                };
            }

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            return new Message<string>
            {
                Status = "S",
                Text = "Password reset successfully. You can now login."
            };
        }


        public Task<List<UsersForDropdown>> GetAllUsersForListAsync()
        {
            return _context.Users
                .Where(c => c.IsActive == true)
                .Select(c => new UsersForDropdown
                {
                    Id = c.Id,
                    Name = c.Name,
                })
                .ToListAsync();
        }
    }
}
