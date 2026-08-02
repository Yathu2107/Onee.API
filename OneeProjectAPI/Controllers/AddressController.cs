using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProjectAPI.Controllers
{
    [Authorize]
    [ApiController]
    public class AddressController(AddressService addressService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/address";
        private readonly AddressService _addressService = addressService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpGet]
        [Route(API_ROUTE_NAME + "/list")]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int items_per_page = 10,
            [FromQuery] string? userId = null)
        {
            var result = await _addressService.GetAllWithPagination(page, items_per_page, userId);
            return Ok(result);
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/by-user/{userId}")]
        public async Task<IActionResult> ByUser(string userId)
        {
            var list = await _addressService.GetByUserAsync(userId);
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Addresses loaded.",
                Code = "200",
                Result = list
            });
        }

        [HttpGet]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var view = await _addressService.GetByIdAsync(id);
            if (view == null)
            {
                return NotFound(new Message<string>
                {
                    Status = "E",
                    Text = "Address not found.",
                    Code = "404"
                });
            }
            return Ok(new Message<object>
            {
                Status = "S",
                Text = "Address loaded.",
                Code = "200",
                Result = view
            });
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/add/{userId}")]
        public async Task<IActionResult> Add(string userId, [FromBody] SavedAddressModelForInsert model)
        {
            var result = await _addressService.AddAsync(model, userId, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id:int}/update/{userId}")]
        public async Task<IActionResult> Update(
            int id,
            string userId,
            [FromBody] SavedAddressModelForUpdate model)
        {
            var result = await _addressService.UpdateAsync(id, model, userId, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/set-default/{userId}")]
        public async Task<IActionResult> SetDefault(int id, string userId)
        {
            var result = await _addressService.SetDefaultAsync(id, userId, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpDelete]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _addressService.DeleteAsync(id);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
