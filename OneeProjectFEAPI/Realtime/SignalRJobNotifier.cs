using Microsoft.AspNetCore.SignalR;
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

            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);
        }

        public async Task NotifyJobUpdatedAsync(JobModelForDetailView job)
        {
            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);

            if (!string.IsNullOrEmpty(job.FK_worker_ID))
            {
                await _hub.Clients.Group(JobHub.UserGroup(job.FK_worker_ID))
                    .SendAsync("JobUpdated", job);
            }

            await _hub.Clients.Group(JobHub.JobGroup(job.Id))
                .SendAsync("JobUpdated", job);
        }

        public async Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message)
        {
            // Fan out to job room for live chat UI, but only ping the
            // recipient's personal group (never the sender).
            await _hub.Clients.Group(JobHub.JobGroup(jobId))
                .SendAsync("ChatMessage", message);

            var sender = (message.FK_sender_ID ?? string.Empty).Trim();
            var customer = (customerId ?? string.Empty).Trim();
            var worker = (workerId ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(customer) &&
                !string.Equals(sender, customer, StringComparison.OrdinalIgnoreCase))
            {
                await _hub.Clients.Group(JobHub.UserGroup(customer))
                    .SendAsync("ChatMessage", message);
            }

            if (!string.IsNullOrEmpty(worker) &&
                !string.Equals(sender, worker, StringComparison.OrdinalIgnoreCase))
            {
                await _hub.Clients.Group(JobHub.UserGroup(worker))
                    .SendAsync("ChatMessage", message);
            }
        }
    }
}
