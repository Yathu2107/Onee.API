using OneeProject.Database.Context;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services.Realtime
{
    /// <summary>
    /// Writes in-app inbox rows, then forwards to SignalR/FCM (or null) notifier.
    /// </summary>
    public class PersistingJobRealtimeNotifier(
        IJobRealtimeNotifier inner,
        NotificationService notificationService) : IJobRealtimeNotifier
    {
        private readonly IJobRealtimeNotifier _inner = inner;
        private readonly NotificationService _notificationService = notificationService;

        public async Task NotifyJobOfferAsync(JobModelForDetailView job)
        {
            if (!string.IsNullOrEmpty(job.FK_worker_ID))
            {
                await _notificationService.CreateInboxAsync(
                    job.FK_worker_ID,
                    "New Onee job offer",
                    Truncate(job.Problem_Text, 80),
                    NotificationTypes.JobOffer,
                    job.Id);
            }

            // Do not create a customer inbox row on offer — they initiated the job.

            await _inner.NotifyJobOfferAsync(job);
        }

        public async Task NotifyJobUpdatedAsync(JobModelForDetailView job)
        {
            await _notificationService.CreateInboxAsync(
                job.FK_customer_ID,
                "Job update",
                $"Your job is now {job.Status}.",
                NotificationTypes.JobUpdated,
                job.Id);

            if (!string.IsNullOrEmpty(job.FK_worker_ID)
                && job.Status != JobStatuses.Offering)
            {
                await _notificationService.CreateInboxAsync(
                    job.FK_worker_ID,
                    "Job update",
                    $"Job #{job.Id} is now {job.Status}.",
                    NotificationTypes.JobUpdated,
                    job.Id);
            }

            await _inner.NotifyJobUpdatedAsync(job);
        }

        public async Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message)
        {
            var recipient = JobNotificationHelper.ResolveChatRecipient(
                customerId,
                workerId,
                message.FK_sender_ID);
            if (!string.IsNullOrEmpty(recipient))
            {
                await _notificationService.CreateInboxAsync(
                    recipient,
                    "New message",
                    Truncate(message.Message, 80),
                    NotificationTypes.ChatMessage,
                    jobId);
            }

            await _inner.NotifyChatMessageAsync(jobId, customerId, workerId, message);
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max] + "...";
        }
    }
}
