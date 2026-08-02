using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Push;
using OneeProject.Services.Services.Realtime;

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

            if (!string.IsNullOrEmpty(job.FK_worker_ID))
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

            var recipient = ResolveChatRecipient(customerId, workerId, message.FK_sender_ID);
            if (!string.IsNullOrEmpty(recipient))
            {
                await _fcm.SendToUserAsync(
                    recipient,
                    "New message",
                    Truncate(message.Message, 80),
                    data);
            }
        }

        /// <summary>Returns the other party on the job; never the sender.</summary>
        internal static string? ResolveChatRecipient(
            string customerId,
            string? workerId,
            string? senderId)
        {
            var sender = (senderId ?? string.Empty).Trim();
            var customer = (customerId ?? string.Empty).Trim();
            var worker = (workerId ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(sender))
                return null;

            string? recipient;
            if (string.Equals(sender, customer, StringComparison.OrdinalIgnoreCase))
                recipient = string.IsNullOrEmpty(worker) ? null : worker;
            else if (string.Equals(sender, worker, StringComparison.OrdinalIgnoreCase))
                recipient = string.IsNullOrEmpty(customer) ? null : customer;
            else
            {
                // Unknown sender — prefer non-sender party.
                if (!string.IsNullOrEmpty(customer) &&
                    !string.Equals(sender, customer, StringComparison.OrdinalIgnoreCase))
                    recipient = customer;
                else if (!string.IsNullOrEmpty(worker) &&
                         !string.Equals(sender, worker, StringComparison.OrdinalIgnoreCase))
                    recipient = worker;
                else
                    recipient = null;
            }

            if (!string.IsNullOrEmpty(recipient) &&
                string.Equals(recipient, sender, StringComparison.OrdinalIgnoreCase))
                return null;

            return recipient;
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max] + "...";
        }
    }
}
