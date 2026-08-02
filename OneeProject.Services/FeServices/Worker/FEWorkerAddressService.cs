using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.Worker
{
    public class FEWorkerAddressService(AddressService addressService)
    {
        private readonly AddressService _addressService = addressService;

        public Task<List<SavedAddressModelForView>> GetMineAsync(string userId)
            => _addressService.GetByUserAsync(userId);

        public async Task<Message<SavedAddressModelForView>> GetAsync(int id, string userId)
        {
            var view = await _addressService.GetByIdAsync(id, userId);
            if (view == null)
                return new Message<SavedAddressModelForView>
                {
                    Status = "E",
                    Text = "Address not found.",
                    Code = "404"
                };
            return new Message<SavedAddressModelForView>
            {
                Status = "S",
                Text = "Address loaded.",
                Code = "200",
                Result = view
            };
        }

        public Task<Message<SavedAddressModelForView>> AddAsync(
            SavedAddressModelForInsert model,
            string userId)
            => _addressService.AddAsync(model, userId, userId);

        public Task<Message<SavedAddressModelForView>> UpdateAsync(
            int id,
            SavedAddressModelForUpdate model,
            string userId)
            => _addressService.UpdateAsync(id, model, userId, userId);

        public Task<Message<string>> SetDefaultAsync(int id, string userId)
            => _addressService.SetDefaultAsync(id, userId, userId);

        public Task<Message<string>> DeleteAsync(int id, string userId)
            => _addressService.DeleteAsync(id, userId);
    }
}
