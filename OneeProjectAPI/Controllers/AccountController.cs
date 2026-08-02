using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class AccountController(AccountService accountService, UserManager<AppUser> userManager) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/accounts";
        private readonly AccountService _accountService = accountService;
        private readonly UserManager<AppUser> _userManager = userManager;

        #region Version 1.0
        /// <summary>
        /// User Registration for Admin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/SignUp")]
        public async Task<IActionResult> RegisterAdmin([FromForm] UserRegistration model)
        {
            var (success, message) = await _accountService.RegisterAdminAsync(model);

            var response = new Message<string>
            {
                Status = success ? "S" : "E",
                Text = message,
                Code = success ? "200" : "REG_FAILED",
                Result = success ? model.Email + " has been registered successfully" : string.Empty
            };

            return success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// User Login
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>Authentication response</returns>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/Login")]
        public async Task<IActionResult> Login([FromBody] TokenRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Message<string>
                {
                    Status = "E",
                    Text = "Invalid login request.",
                    Code = "400",
                    Result = null
                });
            }

            // Call service
            var authModel = await _accountService.LoginAsync(model);

            if (authModel == null)
            {
                return BadRequest(new Message<AuthenticationModel>
                {
                    Status = "E",
                    Text = "Invalid email or password.",
                    Code = "400",
                    Result = null
                });
            }

            // Case 1: Login failed (wrong password or user not found)
            if (string.IsNullOrEmpty(authModel.Token))
            {
                return BadRequest(new Message<AuthenticationModel>
                {
                    Status = "E",
                    Text = authModel.Message, // "Invalid email or password"
                    Code = "400",
                    Result = authModel
                });
            }

            // Case 2: Successful login, set refresh token cookie
            if (!string.IsNullOrEmpty(authModel.RefreshToken))
            {
                SetRefreshTokenInCookie(authModel.RefreshToken);
            }

            return Ok(new Message<AuthenticationModel>
            {
                Status = "S",
                Text = "Login successful.",
                Code = "200",
                Result = authModel
            });
        }

        /// <summary>
        /// Logout current user and mark as offline.
        /// </summary>
        [HttpPost]
        [Route(API_ROUTE_NAME + "/Logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            var response = await _accountService.LogoutAsync(userId);

            if (response.Status != "S")
            {
                if (response.Code == "404") return NotFound(response);
                return BadRequest(response);
            }

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth/refresh-token"
            });

            return Ok(response);
        }

        private void SetRefreshTokenInCookie(string refreshToken)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth/refresh-token",
                Expires = DateTime.UtcNow.AddDays(10),
                MaxAge = TimeSpan.FromDays(10),
                IsEssential = true
            };

            Response.Cookies.Append("refreshToken", refreshToken, options);
        }

        /// <summary>
        /// Get User Details for Top Bar
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-user-details")]
        public async Task<IActionResult> GetUserDetails()
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    Status = "Error",
                    Message = "Invalid token. User ID not found."
                });
            }

            var userDetails = await _accountService.GetUserDetailsAsync(userId);

            if (userDetails == null)
            {
                return NotFound(new
                {
                    Status = "Error",
                    Message = "User not found"
                });
            }

            return Ok(userDetails);
        }

        /// <summary>
        /// Get All Accounts with Pagination
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all-accounts/{tid}")]
        public async Task<IActionResult> GetAllAccountWithPagination(
        string tid,
        int page = 1,
        int items_per_page = 10,
        string search = null,
        string status = null)
        {
            var Data = await _accountService.GetAllAccountsAsync(tid, page, items_per_page, search, status);

            var paginationHelper = new FEPaginationHelper<AccountDetailsModelForTable>(items_per_page, Data.Count);
            var paginationInfo = paginationHelper.GetPaginationInfo(page);

            var payload = new Payload
            {
                Pagination = paginationInfo
            };

            var response = new DataResponse<List<AccountDetailsModelForTable>>
            {
                Data = Data.Accounts,
                Payload = payload
            };

            return Ok(response);
        }

        /// <summary>
        /// Get Account Details by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id}")]
        public async Task<IActionResult> GetAccountDetailsById(string id)
        {
            var accountDetails = await _accountService.GetAccountDetailsById(id);

            if (accountDetails == null)
            {
                var notFoundResponse = new Message<string>
                {
                    Text = "Account not found.",
                    Code = "400",
                };
                return BadRequest(notFoundResponse);
            }

            return Ok(accountDetails);
        }

        /// <summary>
        /// Update Account Details
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id}/Update")]
        public async Task<IActionResult> UpdateAccountDetails(string id, [FromForm] AccountDetailsModelForUpdate model)
        {
            model.LastUpdatedBy = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            model.LastUpdatedOn = CommonResources.LocalDatetime();
            var response = await _accountService.UpdateAccountDetailsAsync(model, id);

            if (response.Status != "S")
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Request OTP for Forget passwprd
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _accountService.SendForgotPasswordOtpAsync(request.Email);
            return Ok(result);
        }

        /// <summary>
        /// Verify the OTP
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _accountService.VerifyOtpAsync(request.Email, request.Otp);
            return Ok(result);
        }

        /// <summary>
        /// Reset the password
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _accountService.ResetPasswordAsync(request.Email, request.NewPassword);
            return Ok(result);
        }

        /// <summary>
        /// Get All users for Dropdown
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all-users-list")]
        public async Task<IActionResult> GetAllCategoriesList()
        {
            var users = await _accountService.GetAllUsersForListAsync();
            if (users == null || !users.Any())
            {
                var notFoundResponse = new Message<string>
                {
                    Text = "No Users found.",
                    Code = "400",
                };
                return BadRequest(notFoundResponse);
            }
            return Ok(users);
        }

        /// <summary>
        /// Get All users by role for Dropdown
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-all-users-list/{id}")]
        public async Task<IActionResult> GetAllUserListByRole( string id)
        {
            var users = await _accountService.GetAllUsersForListByRoleAsync(id);
            if (users == null || !users.Any())
            {
                var notFoundResponse = new Message<string>
                {
                    Text = "No Users found.",
                    Code = "400",
                };
                return BadRequest(notFoundResponse);
            }
            return Ok(users);
        }

        /// <summary>
        /// Set current location for existing user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id}/set-location")]
        public async Task<IActionResult> SetLocation(string id, [FromBody] Location model)
        {
            var adminId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            var response = await _accountService.SetLocation(model, id, adminId);

            if (response.Status != "S")
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        #endregion
    }
}
