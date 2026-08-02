using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.FEAPI_Model.User;
using OneeProject.Services.FeServices;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FEAccountController(FEAccountService feAccountService, DeviceTokenService deviceTokenService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/accounts";
        private readonly FEAccountService _feAccountService = feAccountService;
        private readonly DeviceTokenService _deviceTokenService = deviceTokenService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        #region Version 1.0 APIs

        /// <summary>
        /// Verify phone and send OTP via SMS (Text.lk).
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromQuery] string phone)
        {
            var result = await _feAccountService.VerifyPhoneAsync(phone);
            if (result.Status != "S")
            {
                if (result.Code == "WRONG_APP") return StatusCode(403, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Verify OTP for login/registration. Returns JWT on success.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] FEVerifyOtpRequest model)
        {
            var result = await _feAccountService.VerifyOtpAndProceedAsync(model.PhoneNumber, model.Otp);
            if (result.Status != "S")
            {
                if (result.Code == "WRONG_APP") return StatusCode(403, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Complete registration for new users after OTP.
        /// </summary>
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

            var (success, message) = await _feAccountService.RegisterUserAsync(model, CurrentUserId);
            var response = new Message<string>
            {
                Status = success ? "S" : "E",
                Text = message,
                Code = success ? "REG_SUCCESS" : "REG_FAILED",
                Result = success ? $"{(model.Email ?? model.PhoneNumber)} registered successfully." : string.Empty
            };

            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/update-user")]
        public async Task<IActionResult> UpdateUser([FromForm] FEUpdateUserRequest model)
        {
            var result = await _feAccountService.UpdateUserAsync(model, CurrentUserId);
            if (result.Status != "S")
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/set-location")]
        public async Task<IActionResult> SetLocation([FromBody] FELocationUpdateModel model)
        {
            var result = await _feAccountService.UpdateLocationAsync(model, CurrentUserId);
            if (result.Status != "S")
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/set-online-status")]
        public async Task<IActionResult> SetOnlineStatus([FromBody] FEOnlineStatusModel model)
        {
            var result = await _feAccountService.SetOnlineStatusAsync(CurrentUserId, model.IsOnline);
            if (result.Status != "S")
            {
                if (result.Code == "404") return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-logged-user-details")]
        public async Task<IActionResult> GetUserDetails()
        {
            var userDetails = await _feAccountService.GetUserDetailsAsync(CurrentUserId);
            if (userDetails == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "User not found",
                    Code = "USER_NOT_FOUND",
                    Result = null
                });
            }
            return Ok(userDetails);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/Logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _feAccountService.LogoutAsync(CurrentUserId);
            if (result.Status != "S")
            {
                if (result.Code == "404") return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/register-device-token")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] FEDeviceTokenRequest model)
        {
            var (success, message) = await _deviceTokenService.RegisterAsync(
                CurrentUserId, model.Token, model.Platform);
            var response = new Message<string>
            {
                Status = success ? "S" : "E",
                Text = message,
                Code = success ? "200" : "400",
                Result = string.Empty
            };
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route(API_ROUTE_NAME + "/device-token")]
        public async Task<IActionResult> RemoveDeviceToken([FromBody] FEDeviceTokenRequest model)
        {
            var (success, message) = await _deviceTokenService.RemoveAsync(CurrentUserId, model.Token);
            var response = new Message<string>
            {
                Status = success ? "S" : "E",
                Text = message,
                Code = success ? "200" : "400",
                Result = string.Empty
            };
            return success ? Ok(response) : BadRequest(response);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string phone)
        {
            var result = await _feAccountService.ResendOtpAsync(phone);
            if (result.Status != "S")
            {
                if (result.Code == "WRONG_APP") return StatusCode(403, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        #endregion
    }
}
