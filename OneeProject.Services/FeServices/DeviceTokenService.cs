using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;

namespace OneeProject.Services.FeServices
{
    /// <summary>
    /// Shared device-token persistence for User and Worker apps (FCM).
    /// </summary>
    public class DeviceTokenService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<(bool Success, string Message)> RegisterAsync(
            string userId,
            string token,
            string platform)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Token is required.");

            platform = string.IsNullOrWhiteSpace(platform) ? "android" : platform.Trim().ToLowerInvariant();
            token = token.Trim();
            var now = CommonResources.LocalDatetime();

            var existing = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (existing != null)
            {
                existing.FK_user_ID = userId;
                existing.Platform = platform;
                existing.LastUpdatedOn = now;
            }
            else
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    FK_user_ID = userId,
                    Token = token,
                    Platform = platform,
                    CreatedOn = now,
                    LastUpdatedOn = now
                });
            }

            await _context.SaveChangesAsync();
            return (true, "Device token registered.");
        }

        public async Task<(bool Success, string Message)> RemoveAsync(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Token is required.");

            var existing = await _context.DeviceTokens
                .FirstOrDefaultAsync(t => t.Token == token.Trim() && t.FK_user_ID == userId);

            if (existing == null)
                return (true, "Device token already removed.");

            _context.DeviceTokens.Remove(existing);
            await _context.SaveChangesAsync();
            return (true, "Device token removed.");
        }

        public async Task<List<string>> GetTokensForUserAsync(string userId) =>
            await _context.DeviceTokens
                .Where(t => t.FK_user_ID == userId)
                .Select(t => t.Token)
                .ToListAsync();

        public async Task RemoveTokensAsync(IEnumerable<string> tokens)
        {
            var list = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
            if (list.Count == 0) return;

            var rows = await _context.DeviceTokens.Where(t => list.Contains(t.Token)).ToListAsync();
            if (rows.Count == 0) return;

            _context.DeviceTokens.RemoveRange(rows);
            await _context.SaveChangesAsync();
        }
    }
}
