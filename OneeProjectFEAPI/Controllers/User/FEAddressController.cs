using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.FeServices.User;

namespace OneeProjectFEAPI.Controllers.User
{
    [Authorize]
    [ApiController]
    public class FEAddressController(FEAddressService addressService) : ControllerBase
    {
        private const string API_ROUTE_NAME = "api/v1/addresses";
        private readonly FEAddressService _addressService = addressService;

        private string CurrentUserId =>
            User.Identities.First().Claims.Single(s => s.Type == "uid").Value;

        [HttpGet]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> Mine()
        {
            var list = await _addressService.GetMineAsync(CurrentUserId);
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
            var result = await _addressService.GetAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME)]
        public async Task<IActionResult> Add([FromBody] SavedAddressModelForInsert model)
        {
            var result = await _addressService.AddAsync(model, CurrentUserId);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SavedAddressModelForUpdate model)
        {
            var result = await _addressService.UpdateAsync(id, model, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Route(API_ROUTE_NAME + "/{id:int}/set-default")]
        public async Task<IActionResult> SetDefault(int id)
        {
            var result = await _addressService.SetDefaultAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }

        [HttpDelete]
        [Route(API_ROUTE_NAME + "/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _addressService.DeleteAsync(id, CurrentUserId);
            if (result.Code == "404") return NotFound(result);
            return result.Status == "S" ? Ok(result) : BadRequest(result);
        }
    }
}
