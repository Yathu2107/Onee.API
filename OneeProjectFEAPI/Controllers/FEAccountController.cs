using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Database.Model.FEAPI_Model;
using OneeProject.Services.FeServices;

namespace OneeProjectFEAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class FEAccountController(FEAccountService feAccountService, UserManager<AppUser> userManager) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/accounts";
        private readonly FEAccountService _feAccountService = feAccountService;
        private readonly UserManager<AppUser> _userManager = userManager;

        #region Version 1.0 APIs

        /// <summary>
        /// Verify mobile number. Determines the next step based user status.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-mobile")]
        public async Task<IActionResult> VerifyMobile(
            [FromQuery] string mobile,
            [FromQuery] string countryCode = "+94")
        {
            var validationResult = Helper.Validation.ValidateMobile(mobile, countryCode);
            if (validationResult != null)
                return BadRequest(validationResult);

            var result = await _feAccountService.VerifyMobileAsync(mobile);
            return Ok(result);
        }

        /// <summary>
        /// Verify OTP for login/registration flow. Returns JWT on success.
        /// Creates a stub DB record for new users automatically.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] FEVerifyOtpRequest model,
            [FromQuery] string mode = "WEB")
        {
            var result = await _feAccountService.VerifyOtpAndProceedAsync(model.Mobile, model.Otp);

            if (result.Status != "S")
                return BadRequest(result);

            return Ok(result);
        }


        /// <summary>
        /// Complete registration for new users. Requires valid JWT issued after OTP verification.
        /// WEB: password is mandatory. APP: password is optional.
        /// </summary>
        [Authorize]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/register")]
        public async Task<IActionResult> RegisterUser([FromForm] InsertUser model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Message<string>
                {
                    Status = "E",
                    Text = "Invalid request data",
                    Code = "INVALID_MODEL",
                    Result = string.Empty
                });
            }

            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

            // Register user
            var (success, message) = await _feAccountService.RegisterUserAsync(model, userId);

            var response = new Message<string>
            {
                Status = success ? "S" : "E",
                Text = message,
                Code = success ? "REG_SUCCESS" : "REG_FAILED",
                Result = success ? $"{(model.Email ?? model.PhoneNumber)} registered successfully." : string.Empty
            };

            return success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Update user profile (name, image, address, etc.).
        /// Password change allowed in WEB mode only.
        /// </summary>
        [Authorize]
        [HttpPut]
        [Route(API_ROUTE_NAME + "/update-user")]
        public async Task<IActionResult> UpdateUser([FromForm] FEUpdateUserRequest model)
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;
            var result = await _feAccountService.UpdateUserAsync(model, userId);

            if (result.Status != "S")
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get logged-in user details. Requires valid JWT.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-logged-user-details")]
        public async Task<IActionResult> GetUserDetails()
        {
            var userId = User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new Message<string>
                {
                    Status = "Error",
                    Text = "Invalid token. User ID not found.",
                    Code = "INVALID",
                    Result = null
                });
            }

            var userDetails = await _feAccountService.GetUserDetailsAsync(userId);

            if (userDetails == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "Error",
                    Text = "User not found",
                    Code = "USER_NOT_FOUND",
                    Result = null
                });
            }

            return Ok(userDetails);
        }

        /// <summary>
        /// Resend OTP for login or forgot-password flow.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/resend-otp")]
        public async Task<IActionResult> ResendOtp(
            [FromQuery] string mobile,
            [FromQuery] string countryCode = "+94")
        {
            var validationResult = Helper.Validation.ValidateMobile(mobile, countryCode);
            if (validationResult != null)
                return BadRequest(validationResult);

            var result = await _feAccountService.ResendOtpAsync(mobile);

            if (result.Status != "S")
                return BadRequest(result);

            return Ok(result);
        }
        #endregion
    }
}
