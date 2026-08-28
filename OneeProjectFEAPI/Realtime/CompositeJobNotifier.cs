using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Realtime;
using OneeProject.Services.Services.Push;

namespace OneeProjectFEAPI.Realtime
{
    public class CompositeJobNotifier(
        SignalRJobNotifier signalR,
        FcmPushService fcm) : IJobRealtimeNotifier
    {
        private readonly SignalRJobNotifier _signalR = signalR;
        private readonly FcmPushService _fcm = fcm;

        public async Task NotifyJobOfferAsync(JobModelForDetailView job)
        {
            await _signalR.NotifyJobOfferAsync(job);

            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return;

            var data = new Dictionary<string, string>
            {
                ["type"] = "job_offer",
                ["jobId"] = job.Id.ToString(),
                ["status"] = job.Status,
                ["problemText"] = Truncate(job.Problem_Text, 120),
                ["expiresAt"] = job.Offer_Expires_At?.ToString("o") ?? ""
            };

            // Offer push is worker-only. Customer already knows they created the job.
            await _fcm.SendToUserAsync(
                job.FK_worker_ID,
                "New Onee job offer",
                Truncate(job.Problem_Text, 80),
                data);
        }

        public async Task NotifyJobUpdatedAsync(JobModelForDetailView job)
        {
            await _signalR.NotifyJobUpdatedAsync(job);

            var data = new Dictionary<string, string>
            {
                ["type"] = "job_updated",
                ["jobId"] = job.Id.ToString(),
                ["status"] = job.Status,
                ["amount"] = job.Amount?.ToString() ?? ""
            };

            await _fcm.SendToUserAsync(
                job.FK_customer_ID,
                "Job update",
                $"Your job is now {job.Status}.",
                data);

            // Offering updates go to the current worker via JobOffer only.
            if (!string.IsNullOrEmpty(job.FK_worker_ID)
                && job.Status != JobStatuses.Offering)
            {
                await _fcm.SendToUserAsync(
                    job.FK_worker_ID,
                    "Job update",
                    $"Job #{job.Id} is now {job.Status}.",
                    data);
            }
        }

        public async Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message)
        {
            await _signalR.NotifyChatMessageAsync(jobId, customerId, workerId, message);

            var data = new Dictionary<string, string>
            {
                ["type"] = "chat_message",
                ["jobId"] = jobId.ToString(),
                ["senderId"] = message.FK_sender_ID,
                ["message"] = Truncate(message.Message, 100)
            };

            var recipient = JobNotificationHelper.ResolveChatRecipient(
                customerId,
                workerId,
                message.FK_sender_ID);
            if (!string.IsNullOrEmpty(recipient))
            {
                await _fcm.SendToUserAsync(
                    recipient,
                    "New message",
                    Truncate(message.Message, 80),
                    data);
            }
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max] + "...";
        }
    }
}
