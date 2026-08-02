namespace OneeProject.Services.Services.Realtime
{
    public interface IPushNotificationSender
    {
        Task SendToUserAsync(
            string userId,
            string title,
            string body,
            Dictionary<string, string> data);
    }

    public class NullPushNotificationSender : IPushNotificationSender
    {
        public Task SendToUserAsync(
            string userId,
            string title,
            string body,
            Dictionary<string, string> data) => Task.CompletedTask;
    }
}
