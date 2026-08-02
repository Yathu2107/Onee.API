using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.FEAPI_Model.Worker;
using OneeProject.Services.FeServices;
using OneeProject.Services.FeServices.Worker;

namespace OneeProjectFEAPI.Controllers.Worker
{
    [Authorize]
    [ApiController]
    public class FEWorkerAccountController(
        FEWorkerAccountService workerAccountService,
        DeviceTokenService deviceTokenService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/worker/accounts";
        private readonly FEWorkerAccountService _workerAccountService = workerAccountService;
        private readonly DeviceTokenService _deviceTokenService = deviceTokenService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        #region Version 1.0 APIs

        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromQuery] string phone)
        {
            var result = await _workerAccountService.VerifyPhoneAsync(phone);
            if (result.Status != "S")
            {
                if (result.Code == "WRONG_APP") return StatusCode(403, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route(API_ROUTE_NAME + "/verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] FEWorkerVerifyOtpRequest model)
        {
            var result = await _workerAccountService.VerifyOtpAndProceedAsync(model.PhoneNumber, model.Otp);
            if (result.Status != "S")
            {
                if (result.Code == "WRONG_APP") return StatusCode(403, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/register")]
        public async Task<IActionResult> RegisterWorker([FromForm] InsertWorker model)
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

            var (success, message) = await _workerAccountService.RegisterWorkerAsync(model, CurrentUserId);
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
        [Route(API_ROUTE_NAME + "/update-worker")]
        public async Task<IActionResult> UpdateWorker([FromForm] FEUpdateWorkerRequest model)
        {
            var result = await _workerAccountService.UpdateWorkerAsync(model, CurrentUserId);
            if (result.Status != "S")
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/set-location")]
        public async Task<IActionResult> SetLocation([FromBody] FEWorkerLocationUpdateModel model)
        {
            var result = await _workerAccountService.UpdateLocationAsync(model, CurrentUserId);
            if (result.Status != "S")
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/set-online-status")]
        public async Task<IActionResult> SetOnlineStatus([FromBody] FEWorkerOnlineStatusModel model)
        {
            var result = await _workerAccountService.SetOnlineStatusAsync(CurrentUserId, model.IsOnline);
            if (result.Status != "S")
            {
                if (result.Code == "404") return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/get-logged-worker-details")]
        public async Task<IActionResult> GetWorkerDetails()
        {
            var details = await _workerAccountService.GetWorkerDetailsAsync(CurrentUserId);
            if (details == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Worker not found",
                    Code = "USER_NOT_FOUND",
                    Result = null
                });
            }
            return Ok(details);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/Logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _workerAccountService.LogoutAsync(CurrentUserId);
            if (result.Status != "S")
            {
                if (result.Code == "404") return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/register-device-token")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] FEWorkerDeviceTokenRequest model)
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
        public async Task<IActionResult> RemoveDeviceToken([FromBody] FEWorkerDeviceTokenRequest model)
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
            var result = await _workerAccountService.ResendOtpAsync(phone);
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
