using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.User
{
    /// <summary>
    /// User app inbox shows Admin Panel broadcasts only (not job/chat system pushes).
    /// </summary>
    public class FENotificationService(NotificationService notificationService)
    {
        private readonly NotificationService _notificationService = notificationService;
        private const string InboxType = NotificationTypes.AdminBroadcast;

        public Task<List<NotificationModelForView>> GetMineAsync(string userId)
            => _notificationService.GetMineAsync(userId, InboxType);

        public Task<int> GetUnreadCountAsync(string userId)
            => _notificationService.GetUnreadCountAsync(userId, InboxType);

        public Task<Message<string>> MarkReadAsync(int id, string userId)
            => _notificationService.MarkReadAsync(id, userId, InboxType);

        public Task<Message<string>> MarkAllReadAsync(string userId)
            => _notificationService.MarkAllReadAsync(userId, InboxType);
    }
}
