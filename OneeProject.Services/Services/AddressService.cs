using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class AddressService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<List<SavedAddressModelForView>> GetByUserAsync(string userId)
        {
            var list = await _context.SavedAddresses
                .Where(a => a.FK_user_ID == userId)
                .OrderByDescending(a => a.Is_Default)
                .ThenByDescending(a => a.CreatedOn)
                .ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<SavedAddressModelForView?> GetDefaultByUserAsync(string userId)
        {
            var entity = await _context.SavedAddresses
                .FirstOrDefaultAsync(a => a.FK_user_ID == userId && a.Is_Default);
            return entity == null ? null : Map(entity);
        }

        public async Task<SavedAddressModelForView?> GetByIdAsync(int id, string? userId = null)
        {
            var query = _context.SavedAddresses.Where(a => a.Id == id);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.FK_user_ID == userId);

            var entity = await query.FirstOrDefaultAsync();
            return entity == null ? null : Map(entity);
        }

        public async Task<SavedAddressModelWithPagination> GetAllWithPagination(
            int page,
            int itemsPerPage,
            string? userId)
        {
            var query = _context.SavedAddresses.AsQueryable();
            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.FK_user_ID == userId);

            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            return new SavedAddressModelWithPagination
            {
                Count = count,
                Addresses = items.Select(Map).ToList()
            };
        }

        public async Task<Message<SavedAddressModelForView>> AddAsync(
            SavedAddressModelForInsert model,
            string userId,
            string createdBy)
        {
            if (string.IsNullOrWhiteSpace(model.Label) || string.IsNullOrWhiteSpace(model.Address_Line))
                return Err("Label and Address_Line are required.", "400");

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return Err("User not found.", "404");

            var hasAny = await _context.SavedAddresses.AnyAsync(a => a.FK_user_ID == userId);
            var makeDefault = model.Is_Default || !hasAny;

            if (makeDefault)
                await ClearDefaultsAsync(userId);

            var now = CommonResources.LocalDatetime();
            var entity = new SavedAddress
            {
                FK_user_ID = userId,
                Label = model.Label.Trim(),
                Address_Line = model.Address_Line.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Is_Default = makeDefault,
                CreatedBy = createdBy,
                CreatedOn = now
            };

            _context.SavedAddresses.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(Map(entity), "Address saved.");
        }

        public async Task<Message<SavedAddressModelForView>> UpdateAsync(
            int id,
            SavedAddressModelForUpdate model,
            string userId,
            string updatedBy)
        {
            var entity = await _context.SavedAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.FK_user_ID == userId);

            if (entity == null)
                return Err("Address not found.", "404");

            if (string.IsNullOrWhiteSpace(model.Label) || string.IsNullOrWhiteSpace(model.Address_Line))
                return Err("Label and Address_Line are required.", "400");

            if (model.Is_Default)
                await ClearDefaultsAsync(userId);

            entity.Label = model.Label.Trim();
            entity.Address_Line = model.Address_Line.Trim();
            entity.Latitude = model.Latitude;
            entity.Longitude = model.Longitude;
            entity.Is_Default = model.Is_Default || entity.Is_Default;
            entity.LastUpdatedBy = updatedBy;
            entity.LastUpdatedOn = CommonResources.LocalDatetime();

            await _context.SaveChangesAsync();

            return Ok(Map(entity), "Address updated.");
        }

        public async Task<Message<string>> SetDefaultAsync(int id, string userId, string updatedBy)
        {
            var entity = await _context.SavedAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.FK_user_ID == userId);

            if (entity == null)
                return new Message<string> { Status = "E", Text = "Address not found.", Code = "404" };

            await ClearDefaultsAsync(userId);
            entity.Is_Default = true;
            entity.LastUpdatedBy = updatedBy;
            entity.LastUpdatedOn = CommonResources.LocalDatetime();
            await _context.SaveChangesAsync();

            return new Message<string>
            {
                Status = "S",
                Text = "Default address updated.",
                Code = "200",
                Result = entity.Id.ToString()
            };
        }

        public async Task<Message<string>> DeleteAsync(int id, string? userId = null)
        {
            var query = _context.SavedAddresses.Where(a => a.Id == id);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.FK_user_ID == userId);

            var entity = await query.FirstOrDefaultAsync();
            if (entity == null)
                return new Message<string> { Status = "E", Text = "Address not found.", Code = "404" };

            var wasDefault = entity.Is_Default;
            var ownerId = entity.FK_user_ID;
            _context.SavedAddresses.Remove(entity);
            await _context.SaveChangesAsync();

            if (wasDefault)
            {
                var next = await _context.SavedAddresses
                    .Where(a => a.FK_user_ID == ownerId)
                    .OrderByDescending(a => a.CreatedOn)
                    .FirstOrDefaultAsync();
                if (next != null)
                {
                    next.Is_Default = true;
                    await _context.SaveChangesAsync();
                }
            }

            return new Message<string>
            {
                Status = "S",
                Text = "Address deleted.",
                Code = "200",
                Result = id.ToString()
            };
        }

        /// <summary>
        /// Upsert default saved address only (does not write t_user lat/lng).
        /// </summary>
        public async Task<Message<string>> SetLocationAsDefaultAsync(
            string userId,
            double latitude,
            double longitude,
            string updatedBy,
            string? label = null,
            string? addressLine = null)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return new Message<string> { Status = "E", Text = "User not found.", Code = "404" };

            var existing = await _context.SavedAddresses
                .FirstOrDefaultAsync(a => a.FK_user_ID == userId && a.Is_Default);

            var now = CommonResources.LocalDatetime();
            if (existing == null)
            {
                await ClearDefaultsAsync(userId);
                _context.SavedAddresses.Add(new SavedAddress
                {
                    FK_user_ID = userId,
                    Label = string.IsNullOrWhiteSpace(label) ? "Default" : label.Trim(),
                    Address_Line = string.IsNullOrWhiteSpace(addressLine) ? "Current location" : addressLine.Trim(),
                    Latitude = latitude,
                    Longitude = longitude,
                    Is_Default = true,
                    CreatedBy = updatedBy,
                    CreatedOn = now
                });
            }
            else
            {
                existing.Latitude = latitude;
                existing.Longitude = longitude;
                if (!string.IsNullOrWhiteSpace(label))
                    existing.Label = label.Trim();
                if (!string.IsNullOrWhiteSpace(addressLine))
                    existing.Address_Line = addressLine.Trim();
                existing.LastUpdatedBy = updatedBy;
                existing.LastUpdatedOn = now;
            }

            await _context.SaveChangesAsync();

            return new Message<string>
            {
                Status = "S",
                Text = "Location updated successfully.",
                Code = "200",
                Result = userId
            };
        }

        private async Task ClearDefaultsAsync(string userId)
        {
            var defaults = await _context.SavedAddresses
                .Where(a => a.FK_user_ID == userId && a.Is_Default)
                .ToListAsync();
            foreach (var d in defaults)
                d.Is_Default = false;
        }

        private static SavedAddressModelForView Map(SavedAddress a) => new()
        {
            Id = a.Id,
            FK_user_ID = a.FK_user_ID,
            Label = a.Label,
            Address_Line = a.Address_Line,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            Is_Default = a.Is_Default,
            CreatedOn = a.CreatedOn,
            LastUpdatedOn = a.LastUpdatedOn
        };

        private static Message<SavedAddressModelForView> Ok(SavedAddressModelForView view, string text) => new()
        {
            Status = "S",
            Text = text,
            Code = "200",
            Result = view
        };

        private static Message<SavedAddressModelForView> Err(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
