using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.User
{
    public class FENotificationService(NotificationService notificationService)
    {
        private readonly NotificationService _notificationService = notificationService;

        public Task<List<NotificationModelForView>> GetMineAsync(string userId)
            => _notificationService.GetMineAsync(userId);

        public Task<int> GetUnreadCountAsync(string userId)
            => _notificationService.GetUnreadCountAsync(userId);

        public Task<Message<string>> MarkReadAsync(int id, string userId)
            => _notificationService.MarkReadAsync(id, userId);

        public Task<Message<string>> MarkAllReadAsync(string userId)
            => _notificationService.MarkAllReadAsync(userId);
    }
}
