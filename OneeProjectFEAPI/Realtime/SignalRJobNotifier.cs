using Microsoft.AspNetCore.SignalR;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Realtime;
using OneeProjectFEAPI.Hubs;

namespace OneeProjectFEAPI.Realtime
{
    public class SignalRJobNotifier(IHubContext<JobHub> hub) : IJobRealtimeNotifier
    {
        private readonly IHubContext<JobHub> _hub = hub;

        public async Task NotifyJobOfferAsync(JobModelForDetailView job)
        {
            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return;

            await _hub.Clients.Group(JobHub.UserGroup(job.FK_worker_ID))
                .SendAsync("JobOffer", job);

            // Customer live UI (requesting screen) — worker is not notified here.
            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);
        }

        public async Task NotifyJobUpdatedAsync(JobModelForDetailView job)
        {
            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);

            // While Offering, only the current worker receives JobOffer — not JobUpdated.
            if (!string.IsNullOrEmpty(job.FK_worker_ID)
                && job.Status != JobStatuses.Offering)
            {
                await _hub.Clients.Group(JobHub.UserGroup(job.FK_worker_ID))
                    .SendAsync("JobUpdated", job);
            }
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

            if (string.IsNullOrEmpty(recipient))
                return;

            // Deliver only to the recipient's personal group — never the sender or job room.
            await _hub.Clients.Group(JobHub.UserGroup(recipient))
                .SendAsync("ChatMessage", message);
        }
    }
}
